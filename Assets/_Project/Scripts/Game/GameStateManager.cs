using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    /*
     * 플레이어 목록 검색
     * → 랜덤으로 술래 1명 선정
     * → 역할 정보 동기화
     * → 3초 카운트다운
     * → 게임 시작
     * → 승리 조건 확인
     * → 게임 종료 및 승리자 동기화
     */

    public static GameStateManager Instance { get; private set; }

    // 현재 게임 진행 상태
    [SyncVar]
    private GamePhase currentPhase = GamePhase.Waiting;

    // 최초 술래의 Network ID
    [SyncVar]
    private uint chaserNetId;

    // 승리자 이름 (여러 명일 경우 줄바꿈)
    [SyncVar]
    private string winnerNames = "";

    // 술래 승리 여부
    [SyncVar]
    private bool chaserWon = false;
    
    // 게임 시작 카운트다운
    [SyncVar]
    private int countdown = 3;

    // 게임 제한 시간 (기본 10분)
    [SerializeField]
    private float gameDuration = 600f;

    // 서버 기준 게임 종료 시각
    [SyncVar]
    private double gameEndTime;

    // 게임 시작 중복 실행 방지
    private bool isStarting = false;
    private bool isInterrupted = false;
    
    // UI에서 사용할 공개 속성
    public GamePhase CurrentPhase => currentPhase;
    public uint ChaserNetId => chaserNetId;
    public int Countdown => countdown;
    public string WinnerNames => winnerNames;
    public bool ChaserWon => chaserWon;

    // 남은 게임 시간
    public double RemainingTime =>
        currentPhase == GamePhase.Playing
            ? System.Math.Max(0, gameEndTime - NetworkTime.time)
            : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 서버에서 GameStateManager 시작
    public override void OnStartServer()
    {
        base.OnStartServer();

        currentPhase = GamePhase.Waiting;
        chaserNetId = 0;
        winnerNames = "";
        chaserWon = false;
        countdown = 3;
        gameEndTime = 0;
        isStarting = false;
        isInterrupted = false;
        
        Debug.Log("[GameState] 플레이어 준비 대기");
    }

    // 모든 플레이어 스폰 완료 후 호출
    [Server]
    public void StartGameSequence()
    {
        Debug.Log(
            $"[GameState] StartGameSequence 호출! " +
            $"Phase={currentPhase}, isStarting={isStarting}"
        );

        if (currentPhase != GamePhase.Waiting)
        {
            Debug.LogWarning(
                $"[GameState] 시작 차단: Phase={currentPhase}"
            );
            return;
        }

        if (isStarting)
        {
            Debug.LogWarning(
                "[GameState] 시작 차단: 이미 시작 중"
            );
            return;
        }

        isStarting = true;
        winnerNames = "";

        Debug.Log("[GameState] GameStartRoutine 실행");

        StartCoroutine(GameStartRoutine());
    }


    // 술래 선정 및 게임 시작

    [Server]
    private IEnumerator GameStartRoutine()
    {
        Debug.Log("[GameState] 코루틴 내부 진입 성공");

        PlayerRoleSync[] players =
            FindObjectsByType<PlayerRoleSync>(
                    FindObjectsSortMode.None
                )
                .OrderBy(p => p.netId)
                .ToArray();

        Debug.Log(
            $"[GameState] PlayerRoleSync 발견: {players.Length}명"
        );
        if (players.Length == 0)
        {
            Debug.LogError("[GameState] 플레이어가 없습니다.");

            isStarting = false;
            yield break;
        }

        // 모든 플레이어를 도망자로 초기화
        foreach (PlayerRoleSync player in players)
        {
            player.SetRole(PlayerRole.Runner);
            player.SetJailed(false);
        }

        // 술래 1명 무작위 선택
        int randomIndex = Random.Range(0, players.Length);

        PlayerRoleSync chaser = players[randomIndex];

        chaser.SetRole(PlayerRole.Chaser);

        // 최초 술래 정보 저장
        chaserNetId = chaser.netId;

        Debug.Log(
            $"[GameState] 최초 술래 선정: {chaser.netId}"
        );

        // 3초 카운트다운 시작
        currentPhase = GamePhase.Countdown;

        for (int i = 3; i >= 1; i--)
        {
            if (isInterrupted)
                yield break;
            countdown = i;

            Debug.Log(
                $"[GameState] 게임 시작까지 {i}초"
            );

            yield return new WaitForSeconds(1f);
        }
        if (isInterrupted)
            yield break;
        countdown = 0;

        // 서버 시간을 기준으로 게임 종료 시각 설정
        gameEndTime = NetworkTime.time + gameDuration;

        // 게임 시작
        currentPhase = GamePhase.Playing;

        isStarting = false;

        Debug.Log("[GameState] GAME START!");
    }

    // 서버에서 게임 상태 확인
    [ServerCallback]
    private void Update()
    {
        if (isInterrupted || currentPhase != GamePhase.Playing)
            return;

        // 1. 감금 시간이 끝난 플레이어를 탈락시킨다.
        UpdatePrisonTimers();

        // 2. 자유로운 생존 도망자가 남아 있는지 확인한다.
        CheckChaserWin();

        // 술래 승리로 끝났으면 아래 시간 종료 처리를 실행하지 않는다.
        if (currentPhase != GamePhase.Playing)
            return;

        // 3. 전체 시간이 끝날 때까지 버텼으면 도망자 측 승리다.
        if (NetworkTime.time >= gameEndTime)
        {
            Debug.Log("[GameState] 전체 시간 종료 - 도망자 승리");
            FinishGame(false);
        }
    }

    [Server]
    private void CheckChaserWin()
    {
        if (currentPhase != GamePhase.Playing)
            return;

        PlayerRoleSync[] players =
            FindObjectsByType<PlayerRoleSync>(
                FindObjectsSortMode.None
            );

        // 술래 없이 도망자 혼자 진행하는 테스트에서는
        // 전원 감금 승리 판정을 생략해 타이머를 확인할 수 있게 한다.
        bool hasChaser = players.Any(player => player.IsChaser);

        if (!hasChaser)
            return;

        bool hasRunner = false;

        foreach (PlayerRoleSync player in players)
        {
            if (!player.IsRunner)
                continue;

            hasRunner = true;

            PlayerHealth health =
                player.GetComponent<PlayerHealth>();

            // 필수 컴포넌트가 빠진 경우 잘못된 승리 처리를 막는다.
            if (health == null)
            {
                Debug.LogError(
                    "[GameState] 도망자에게 PlayerHealth가 없습니다.",
                    player
                );
                return;
            }

            // 이미 탈락한 사람은 생존자 검사에서 제외한다.
            if (health.IsDead)
                continue;

            // 자유로운 생존 도망자가 한 명이라도 있으면 계속 진행한다.
            if (!player.IsJailed)
                return;
        }

        // 도망자 자체가 없는 테스트 상황에서는 종료하지 않는다.
        if (!hasRunner)
            return;

        Debug.Log(
            "[GameState] 자유로운 생존 도망자 없음 - 술래 승리"
        );

        FinishGame(true);
    }

    // 승리자 이름 결정
    [Server]
    private string GetWinnerNames(bool chaserWon)
    {
        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            )
            .OrderBy(p => p.netId)
            .ToArray();

        // 최초 술래 승리
        if (chaserWon)
        {
            foreach (NetworkPlayer player in players)
            {
                if (player.netId == chaserNetId)
                {
                    return string.IsNullOrWhiteSpace(
                        player.SteamName
                    )
                        ? "CHASER"
                        : player.SteamName;
                }
            }

            return "CHASER";
        }

        // 시간 종료 시 생존 도망자 승리
        string names = "";

        foreach (NetworkPlayer player in players)
        {
            PlayerRoleSync role =
                player.GetComponent<PlayerRoleSync>();

            if (role == null)
                continue;

            // 도망자만 승리
            if (!role.IsRunner)
                continue;

            PlayerHealth health = player.GetComponent<PlayerHealth>();

            // 전체 시간 종료 시에는 완전히 탈락한 도망자를 제외한다.
            if (health != null && health.IsDead)
                continue;

            if (names != "")
                names += "\n";

            string playerName =
                string.IsNullOrWhiteSpace(player.SteamName)
                    ? $"Player {player.netId}"
                    : player.SteamName;

            names += playerName;
        }

        // 혼자 테스트할 때 승리자가 없는 경우
        if (string.IsNullOrEmpty(names))
        {
            return "NO WINNER";
        }

        return names;
    }

    [Server]
    private void FinishGame(bool chaserWon)
    {
        if (currentPhase != GamePhase.Playing)
            return;

        // 승리 팀 저장
        this.chaserWon = chaserWon;

        // 승리자 닉네임 저장
        winnerNames = GetWinnerNames(chaserWon);

        // 게임 종료 상태 동기화
        currentPhase = GamePhase.Finished;

        Debug.Log(
            $"[GameState] 게임 종료! 승리자: {winnerNames}"
        );
    }
    
    [Server]
    public void InterruptGame()
    {
        if (isInterrupted)
            return;

        isInterrupted = true;
        isStarting = false;

        // 게임 타이머 및 승패 판정 중단
        currentPhase = GamePhase.Waiting;
        gameEndTime = 0;

        // 진행 중인 게임 시작 카운트다운 중단
        StopAllCoroutines();

        Debug.Log("[GameState] 플레이어 이탈로 게임 중단");

        RpcShowDisconnectNotice();
    }

    [ClientRpc]
    private void RpcShowDisconnectNotice()
    {
        // 게임 중단 안내를 이미 받았다는 사실 기록
        SteamLobbyManager.Instance?.NotifyGameInterrupted();

        GameReturnNotice notice =
            FindFirstObjectByType<GameReturnNotice>(
                FindObjectsInactive.Include
            );

        if (notice != null)
        {
            notice.ShowNotice();
        }
        else
        {
            Debug.LogWarning(
                "[GameState] GameReturnNotice 없음"
            );
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [Server]
    private void UpdatePrisonTimers()
    {
        // 감옥이 제거되어도 안전하게 순회하도록 목록을 복사한다.
        NetworkIdentity[] identities =
            NetworkServer.spawned.Values.ToArray();

        foreach (NetworkIdentity identity in identities)
        {
            if (identity == null)
                continue;

            NetworkPrison prison =
                identity.GetComponent<NetworkPrison>();

            if (prison != null)
                prison.ServerCheckExpiration(gameEndTime);
        }
    }
}
