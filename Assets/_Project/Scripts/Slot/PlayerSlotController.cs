using System;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRoleSync))]
public class PlayerSlotController : NetworkBehaviour
{
    [Header("슬롯 설정")]

    [SerializeField]
    private SlotProbabilityTable _probabilityTable;

    [SerializeField, Min(0.1f)]
    private float _spinInterval = 3f;

    // ── 서버에서 사용하는 정보 ──

    private PlayerRoleSync _role;
    private SlotRoller _roller;

    private bool _faulted;
    private bool _roundRunning;
    private int _roundId;
    private double _roundEndTime;

    // ── 이전에 확인한 상태 ──

    private bool _hasPreviousState;
    private GameStateManager _previousGame;
    private GamePhase _previousPhase;
    private PlayerRole _previousRole;
    private bool _previousJailed;

    // ── 본인 클라이언트에서 사용하는 정보 ──

    public bool IsSpinning { get; private set; }
    public int CurrentRoundId { get; private set; }
    public double RoundStartTime { get; private set; }
    public double RoundEndTime { get; private set; }

    public bool HasResult { get; private set; }
    public SlotResult LastResult { get; private set; }

    // 나중에 UI에서 구독할 이벤트
    public event Action RoundStarted;
    public event Action RoundCancelled;
    public event Action<SlotResult> ResultReceived;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _role = GetComponent<PlayerRoleSync>();

        _faulted = false;
        _roundRunning = false;
        _roundId = 0;
        _roundEndTime = 0;
        _hasPreviousState = false;

        if (_probabilityTable == null || _role == null)
        {
            _faulted = true;

            Debug.LogError(
                $"[Slot] Player={netId}: " +
                "확률표 또는 PlayerRoleSync가 없습니다.",
                this);

            return;
        }

        _roller = new SlotRoller(_probabilityTable);
    }

    [ServerCallback]
    private void LateUpdate()
    {
        GameStateManager game = GameStateManager.Instance;

        // 기존 코드의 값을 읽어서 상태 변경을 감지합니다.
        CheckStateChanges(game);

        if (!CanRun(game))
        {
            ServerCancelRound("실행 불가 상태");
            return;
        }

        // 시작·석방·역할 변경 이후에는 새 회차를 시작합니다.
        if (!_roundRunning)
        {
            StartRound();
            return;
        }

        // 회차 종료 시각이 될 때까지 기다립니다.
        if (NetworkTime.time < _roundEndTime)
            return;

        CompleteRound();
    }

    [Server]
    private void CheckStateChanges(GameStateManager game)
    {
        if (_role == null)
            return;

        // 게임 관리자가 없으면 대기 상태로 취급합니다.
        GamePhase currentPhase = game != null
            ? game.CurrentPhase
            : GamePhase.Waiting;

        PlayerRole currentRole = _role.Role;
        bool currentJailed = _role.IsJailed;

        if (_hasPreviousState)
        {
            bool gameChanged =
                game != _previousGame ||
                currentPhase != _previousPhase;

            bool roleChanged =
                currentRole != _previousRole;

            bool jailChanged =
                currentJailed != _previousJailed;

            if (gameChanged || roleChanged || jailChanged)
            {
                ServerCancelRound("게임/역할/감옥 상태 변경");
            }
        }

        // 이번에 읽은 값을 다음 비교에 사용합니다.
        _previousGame = game;
        _previousPhase = currentPhase;
        _previousRole = currentRole;
        _previousJailed = currentJailed;
        _hasPreviousState = true;
    }

    private bool CanRun(GameStateManager game)
    {
        if (_faulted || _role == null || _roller == null)
            return false;

        if (connectionToClient == null)
            return false;

        if (game == null ||
            game.CurrentPhase != GamePhase.Playing ||
            game.RemainingTime <= 0)
        {
            return false;
        }

        // 술래는 감옥 상태 값이 남아 있어도 실행 가능합니다.
        // 도망자는 감옥 밖에 있어야 실행 가능합니다.
        return _role.IsChaser || !_role.IsJailed;
    }

    [Server]
    private void StartRound()
    {
        if (float.IsNaN(_spinInterval) ||
            float.IsInfinity(_spinInterval) ||
            _spinInterval <= 0f)
        {
            _faulted = true;

            Debug.LogError(
                $"[Slot] Player={netId}: " +
                "추첨 주기는 유한한 양수여야 합니다.",
                this);

            return;
        }

        _roundId++;
        _roundRunning = true;

        double startTime = NetworkTime.time;
        _roundEndTime = startTime + _spinInterval;

        Debug.Log(
            $"[Slot][Server] Player={netId} " +
            $"Round={_roundId} Started " +
            $"Duration={_spinInterval:F1}s",
            this);

        TargetRoundStarted(
            connectionToClient,
            _roundId,
            startTime,
            _roundEndTime);
    }

    [Server]
    private void CompleteRound()
    {
        SlotResult result;

        try
        {
            result = _roller.Roll();
        }
        catch (InvalidOperationException exception)
        {
            _faulted = true;
            ServerCancelRound("확률표 오류");

            Debug.LogError(exception.Message, this);
            return;
        }

        _roundRunning = false;

        Debug.Log(
            $"[Slot][Server] Player={netId} " +
            $"Round={_roundId} Completed " +
            $"{result.First}/{result.Second}/{result.Third}",
            this);

        TargetRoundCompleted(
            connectionToClient,
            _roundId,
            result);

        // 밀린 회차를 몰아서 처리하지 않고,
        // 현재 시각부터 다음 회차를 시작합니다.
        if (CanRun(GameStateManager.Instance))
        {
            StartRound();
        }
    }

    [Server]
    public void ServerCancelRound(string reason)
    {
        if (!_roundRunning)
            return;

        _roundRunning = false;
        _roundEndTime = 0;

        Debug.Log(
            $"[Slot][Server] Player={netId} " +
            $"Round={_roundId} Cancelled: {reason}",
            this);

        if (connectionToClient != null)
        {
            TargetRoundCancelled(
                connectionToClient,
                _roundId);
        }
    }

    [TargetRpc]
    private void TargetRoundStarted(
        NetworkConnectionToClient target,
        int roundId,
        double startTime,
        double endTime)
    {
        CurrentRoundId = roundId;
        RoundStartTime = startTime;
        RoundEndTime = endTime;
        IsSpinning = true;

        Debug.Log(
            $"[Slot][Owner] Player={netId} " +
            $"Round={roundId} Started",
            this);

        RoundStarted?.Invoke();
    }

    [TargetRpc]
    private void TargetRoundCancelled(
        NetworkConnectionToClient target,
        int roundId)
    {
        if (roundId != CurrentRoundId || !IsSpinning)
            return;

        IsSpinning = false;
        RoundStartTime = 0;
        RoundEndTime = 0;

        Debug.Log(
            $"[Slot][Owner] Player={netId} " +
            $"Round={roundId} Cancelled",
            this);

        RoundCancelled?.Invoke();
    }

    [TargetRpc]
    private void TargetRoundCompleted(
        NetworkConnectionToClient target,
        int roundId,
        SlotResult result)
    {
        if (roundId != CurrentRoundId || !IsSpinning)
            return;

        IsSpinning = false;
        LastResult = result;
        HasResult = true;

        Debug.Log(
            $"[Slot][Owner] Player={netId} " +
            $"Round={roundId} Completed " +
            $"{result.First}/{result.Second}/{result.Third}",
            this);

        ResultReceived?.Invoke(result);
    }

    public override void OnStopServer()
    {
        _roundRunning = false;
        _roundEndTime = 0;
        _hasPreviousState = false;
        _previousGame = null;
        _roller = null;

        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        IsSpinning = false;
        HasResult = false;
        CurrentRoundId = 0;
        RoundStartTime = 0;
        RoundEndTime = 0;
        LastResult = default;

        base.OnStopClient();
    }
}