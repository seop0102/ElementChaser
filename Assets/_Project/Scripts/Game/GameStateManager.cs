
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

    // UI에서 사용할 공개 속성
    public GamePhase CurrentPhase => currentPhase;
    public uint ChaserNetId => chaserNetId;
    public int Countdown => countdown;
    public string WinnerNames => winnerNames;

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
        countdown = 3;
        gameEndTime = 0;
        isStarting = false;

        Debug.Log("[GameState] 플레이어 준비 대기");
    }

    // 모든 플레이어 스폰 완료 후 호출
    [Server]
    public void StartGameSequence()
    {
        if (currentPhase != GamePhase.Waiting)
            return;

        // 중복 시작 방지
        if (isStarting)
            return;

        isStarting = true;
        winnerNames = "";

        StartCoroutine(GameStartRoutine());
    }

    // 술래 선정 및 게임 시작
    [Server]
    private IEnumerator GameStartRoutine()
    {
        PlayerRoleSync[] players =
            FindObjectsByType<PlayerRoleSync>(
                FindObjectsSortMode.None
            )
            .OrderBy(p => p.netId)
            .ToArray();

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
            countdown = i;

            Debug.Log(
                $"[GameState] 게임 시작까지 {i}초"
            );

            yield return new WaitForSeconds(1f);
        }

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
        if (currentPhase != GamePhase.Playing)
            return;

        // 1. 제한 시간 종료
        if (NetworkTime.time >= gameEndTime)
        {
            Debug.Log("[GameState] 제한 시간 종료");

            // 시간 종료 시 생존 도망자 승리
            FinishGame(false);
            return;
        }

        // 2. 도망자가 모두 술래가 되었는지 확인
        CheckChaserWin();
    }

    // 도망자가 모두 술래가 되었는지 검사
    [Server]
    private void CheckChaserWin()
    {
        if (currentPhase != GamePhase.Playing)
            return;

        PlayerRoleSync[] players =
            FindObjectsByType<PlayerRoleSync>(
                FindObjectsSortMode.None
            );

        // 현재 1인 테스트 시 자동 종료 방지
        // 실제 4인 게임의 중도 퇴장 처리는 추후 구현
        if (players.Length < 4)
            return;

        foreach (PlayerRoleSync player in players)
        {
            // Runner가 한 명이라도 남아 있으면 계속 진행
            // 감옥에 있는 Runner도 아직 Runner로 취급
            if (player.IsRunner)
                return;
        }

        Debug.Log(
            "[GameState] 모든 도망자가 술래로 전환됨"
        );

        // 최초 술래 승리
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

            // 감옥에 있는 플레이어는 제외
            if (role.IsJailed)
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

    // 게임 종료
    [Server]
    private void FinishGame(bool chaserWon)
    {
        // 중복 종료 방지
        if (currentPhase != GamePhase.Playing)
            return;

        // 서버에서 승리자 결정
        winnerNames = GetWinnerNames(chaserWon);

        // 모든 클라이언트에 종료 상태 동기화
        currentPhase = GamePhase.Finished;

        Debug.Log(
            $"[GameState] 게임 종료! 승리자: {winnerNames}"
        );
    }
}
