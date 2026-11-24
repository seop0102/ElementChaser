
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct LobbyCountMessage : NetworkMessage
{
    public int count;
}
public struct LobbyCountRequestMessage : NetworkMessage
{
}
public class ElementNetworkManager : NetworkManager
{
    private const int RequiredPlayerCount = 4;
    
    private readonly HashSet<int> loadedPlayers = new HashSet<int>();
    
    private readonly HashSet<uint> spawnedPlayers = new HashSet<uint>();
    
    private bool spawnStarted = false;
    private bool returningToLobby = false;
    private bool isShuttingDown = false;
    private bool gameStartRequested = false; 
    private Coroutine startupWatchdog;
    
    public void BeginShutdown()
    {
        isShuttingDown = true;
        Debug.Log("[Network] 서버 종료 절차 시작");
    }
    public int LobbyPlayerCount { get; private set; } = 0;

    [Server]
    private int GetServerPlayerCount()
    {
        return NetworkServer.connections.Values.Count(
            conn => conn != null && conn.identity != null
        );
    }

    
    [Server]
    private void BroadcastLobbyCount()
    {
        int count = GetServerPlayerCount();

        // 호스트의 로컬 인원수 직접 갱신
        LobbyPlayerCount = count;

        Debug.Log($"[LobbyCount] 서버 인원수: {count}/4");

        // 연결된 클라이언트에게 인원수 전달
        NetworkServer.SendToAll(
            new LobbyCountMessage { count = count }
        );

        // 호스트 로비 UI 직접 갱신
        if (NetworkClient.active)
        {
            LobbyUI lobbyUI =
                FindFirstObjectByType<LobbyUI>(
                    FindObjectsInactive.Include
                );

            if (lobbyUI != null)
                lobbyUI.RefreshLobby();
        }
    }
    [Server]
    private void OnLobbyCountRequested(
        NetworkConnectionToClient conn,
        LobbyCountRequestMessage message)
    {
        int count = GetServerPlayerCount();

        conn.Send(new LobbyCountMessage
        {
            count = count
        });

        Debug.Log(
            $"[LobbyCount] {conn.connectionId}에게 " +
            $"{count}/4 응답"
        );
    }

    private void OnLobbyCountReceived(LobbyCountMessage message)
    {
        LobbyPlayerCount = message.count;

        Debug.Log(
            $"[LobbyCount] 인원수 수신: {LobbyPlayerCount}/4"
        );

        LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>(
            FindObjectsInactive.Include
        );

        if (lobbyUI != null)
            lobbyUI.RefreshLobby();
    }
    
    
    public void StartGame()
    {
        Debug.Log("[StartGame] 버튼으로 게임 시작 요청됨!");
        Debug.Log(
                $"[StartGame] Server={NetworkServer.active}, " +
                $"Client={NetworkClient.active}, " +
                $"Connections={NetworkServer.connections.Count}"
                );
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Only the host can start the game.");
            return;
        }

        if (gameStartRequested ||
            returningToLobby ||
            isShuttingDown ||
            SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.LogWarning("[StartGame] 현재 게임 시작 불가");
            return;
        }

        if (NetworkServer.connections.Count != RequiredPlayerCount)
        {
            Debug.LogWarning(
                $"[StartGame] 연결 인원 부족: " +
                $"{NetworkServer.connections.Count}/{RequiredPlayerCount}"
            );
            return;
        }
        
        int registeredPlayers = GetServerPlayerCount();

        if (registeredPlayers != RequiredPlayerCount)
        {
            Debug.LogWarning(
                $"[StartGame] 플레이어 등록 미완료: " +
                $"{registeredPlayers}/{RequiredPlayerCount}"
            );
            return;
        }
        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        bool namesReady =
            players.Length == RequiredPlayerCount &&
            players.All(p =>
                !string.IsNullOrWhiteSpace(p.SteamName));

        if (!namesReady)
        {
            Debug.LogWarning(
                "[StartGame] 플레이어 닉네임 동기화 대기"
            );
            return;
        }

        gameStartRequested = true; // 추가

        returningToLobby = false;

        SteamLobbyManager.Instance?.ClearReturnToLobby();

        loadedPlayers.Clear();
        spawnedPlayers.Clear();
        spawnStarted = false;

// 기존 게임 시작 감시가 남아 있다면 정리
        if (startupWatchdog != null)
        {
            StopCoroutine(startupWatchdog);
            startupWatchdog = null;
        }

// 게임 시작 과정 감시 시작
        startupWatchdog = StartCoroutine(WatchGameStartup());

// 게임 시작 후 로비 신규 참가 제한
        SteamLobbyManager.Instance?.LockLobby();

// 모든 클라이언트를 게임 씬으로 이동
        ServerChangeScene("GameScene");

    }
    
[Server]
private IEnumerator WatchGameStartup()
{
    float deadline = Time.realtimeSinceStartup + 90f;
    float nextLog = Time.realtimeSinceStartup + 10f;

    Debug.Log("[Connect/Startup] 게임 시작 감시 시작");

    while (NetworkServer.active &&
           !returningToLobby &&
           !isShuttingDown &&
           Time.realtimeSinceStartup < deadline)
    {
        GameStateManager state = GameStateManager.Instance;

        // 카운트다운 또는 실제 플레이 상태라면
        // 게임 시작 단계는 통과한 것으로 판단
        if (state != null &&
            (state.CurrentPhase == GamePhase.Countdown ||
             state.CurrentPhase == GamePhase.Playing))
        {
            Debug.Log(
                $"[Connect/Startup] 시작 성공: {state.CurrentPhase}"
            );

            startupWatchdog = null;
            yield break;
        }

        // 10초마다 현재 준비 상태 출력
        if (Time.realtimeSinceStartup >= nextLog)
        {
            Debug.LogWarning(
                $"[Connect/Startup] 대기 중 | " +
                $"Scene={SceneManager.GetActiveScene().name}, " +
                $"Ready={loadedPlayers.Count}/{RequiredPlayerCount}, " +
                $"SpawnAck={spawnedPlayers.Count}/{RequiredPlayerCount}, " +
                $"Phase={(state == null ? "Missing" : state.CurrentPhase.ToString())}"
            );

            nextLog = Time.realtimeSinceStartup + 10f;
        }

        yield return new WaitForSecondsRealtime(0.5f);
    }

    startupWatchdog = null;

    // 종료 절차가 이미 시작됐으면 중복 처리하지 않음
    if (!NetworkServer.active ||
        returningToLobby ||
        isShuttingDown)
    {
        yield break;
    }

    Debug.LogError(
        $"[Connect/Startup] 90초 초과 | " +
        $"Ready={loadedPlayers.Count}/{RequiredPlayerCount}, " +
        $"SpawnAck={spawnedPlayers.Count}/{RequiredPlayerCount}"
    );

    // 게임 씬에 들어온 경우 기존 중단 절차 사용
    if (SceneManager.GetActiveScene().name == "GameScene")
    {
        BeginGameInterruptShutdown();
    }
    else
    {
        SteamLobbyManager.Instance?.LeaveLobby();
    }
}

    
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName != "GameScene")
            return;

        Debug.Log("[Loading] 서버 GameScene 로딩 완료");

        TryStartSpawn();
    }

    
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        loadedPlayers.Add(conn.connectionId);

        Debug.Log(
            $"[Loading] {loadedPlayers.Count}/{RequiredPlayerCount}명 Ready"
        );

        TryStartSpawn();
    }
    
    [Server]
    private void TryStartSpawn()
    {
        if (spawnStarted)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        if (loadedPlayers.Count != RequiredPlayerCount)
            return;

        spawnStarted = true;

        StartCoroutine(AssignSpawnPositions());
    }
    
    [Server]
    private IEnumerator AssignSpawnPositions()
    {
        PlayerMovement[] players = null;

        float timeout = Time.realtimeSinceStartup + 35f;

        while (Time.realtimeSinceStartup < timeout &&
               spawnStarted &&
               !returningToLobby &&
               !isShuttingDown)
        {
            players = FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

            if (players.Length == RequiredPlayerCount &&
                players.All(p => p.isServer))
            {
                break;
            }

            yield return null;
        }
        if (!spawnStarted || returningToLobby || isShuttingDown)
        {
            Debug.Log("[Spawn] 게임 중단으로 스폰 대기 취소");
            yield break;
        }
        if (players == null ||
            players.Length != RequiredPlayerCount ||
            players.Any(p => !p.isServer))
        {
            Debug.LogError(
                $"[Connect/Spawn] 플레이어 준비 실패 | " +
                $"현재={players?.Length ?? 0}/{RequiredPlayerCount}"
            );

            BeginGameInterruptShutdown();
            yield break;
        }
        
        players = players.OrderBy(p => p.netId).ToArray();
        
        Transform[] spawnPoints = new Transform[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            string spawnName = $"SpawnPoint{i + 1:00}";

            GameObject spawnPoint = GameObject.Find(spawnName);

            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"[Connect/Spawn] 스폰 포인트 없음: {spawnName}"
                );

                BeginGameInterruptShutdown();
                yield break;
            }

            spawnPoints[i] = spawnPoint.transform;
        }
        
        spawnedPlayers.Clear();

        for (int i = 0; i < players.Length; i++)
        {
            Vector3 position =
                spawnPoints[i].position + Vector3.up * 2f;

            players[i].SetSpawnPosition(
                position,
                spawnPoints[i].rotation
            );
        }

        Debug.Log("[Spawn] 모든 플레이어 스폰 위치 배정 완료");
        Debug.Log("[Spawn] 클라이언트 스폰 완료 응답 대기");
    }
    
    [Server]
    public void ConfirmPlayerSpawn(uint playerNetId)
    {
        if (!spawnStarted)
            return;

        if (!NetworkServer.spawned.TryGetValue(
            playerNetId,
            out NetworkIdentity playerIdentity))
        {
            return;
        }
        
        if (playerIdentity.connectionToClient == null ||
            playerIdentity.connectionToClient.identity != playerIdentity)
        {
            Debug.LogWarning(
                $"[Connect/Spawn] 유효하지 않은 스폰 완료 응답: {playerNetId}"
            );
            return;
        }
        if (!spawnedPlayers.Add(playerNetId))
            return;

        Debug.Log(
            $"[Spawn] {spawnedPlayers.Count}/{RequiredPlayerCount}명 스폰 완료"
        );

        if (spawnedPlayers.Count == RequiredPlayerCount)
        {
            StartGameAfterSpawn();
        }
    }
    
    [Server]
    private void StartGameAfterSpawn()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            Debug.LogError(
                "[GameState] GameStateManager를 찾을 수 없습니다."
            );
            return;
        }

        Debug.Log("[Spawn] 모든 플레이어 준비 완료");

        gameState.StartGameSequence();
    }
    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<LobbyCountMessage>(
            OnLobbyCountReceived
        );

        base.OnStartClient();

        Debug.Log("[Network] 인원수 수신 핸들러 준비 완료");
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        Debug.Log("[Network] 클라이언트 연결 성공");

        NetworkClient.Send(new LobbyCountRequestMessage());

        SteamLobbyManager.Instance?.OnMirrorConnected();
    }
    public override void OnServerConnect(
        NetworkConnectionToClient conn)
    {
        bool isGameScene =
            SceneManager.GetActiveScene().name != "MainMenu";

        bool isFull =
            NetworkServer.connections.Count > RequiredPlayerCount;

        if (isGameScene ||
            isFull ||
            isShuttingDown ||
            gameStartRequested ||
            returningToLobby)
        {
            Debug.LogWarning(
                $"[Network] 신규 접속 거절: {conn.connectionId}"
            );

            conn.Disconnect();
            return;
        }

        base.OnServerConnect(conn);
    }
    public override void OnServerAddPlayer(
        NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        Debug.Log(
            $"[Network] 플레이어 등록: {conn.connectionId}"
        );

        BroadcastLobbyCount();
    }
    
    public override void OnClientDisconnect()
    {
        bool wasHost = NetworkServer.active;

        bool wasGameFinished =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentPhase
            == GamePhase.Finished;

        if (!wasHost)
        {
            Debug.Log(
                $"[Network] 클라이언트 연결 종료, " +
                $"GameFinished={wasGameFinished}"
            );

            SteamLobbyManager.Instance?.HandleDisconnected(
                wasGameFinished
            );
        }

        base.OnClientDisconnect();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        LobbyPlayerCount = 0;

        Debug.Log("[Network] Mirror 클라이언트 종료 완료");

        SteamLobbyManager.Instance?.OnMirrorStopped();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        isShuttingDown = false;
        returningToLobby = false;
        spawnStarted = false;
        gameStartRequested = false;
        LobbyPlayerCount = 0;

        loadedPlayers.Clear();
        spawnedPlayers.Clear();

        NetworkServer.RegisterHandler<LobbyCountRequestMessage>(
            OnLobbyCountRequested
        );

        Debug.Log("[Network] 서버 초기화 완료");
    }
    public override void OnStopServer()
    {
        // 종료 과정에서 발생하는 Disconnect 중복 처리 방지
        isShuttingDown = true;
        gameStartRequested = false; 
        if (startupWatchdog != null)
        {
            StopCoroutine(startupWatchdog);
            startupWatchdog = null;
        }
        
        loadedPlayers.Clear();
        spawnedPlayers.Clear();

        base.OnStopServer();

        spawnStarted = false;
        returningToLobby = false;

        LobbyPlayerCount = 0;

        Debug.Log("[Network] 서버 종료 및 상태 초기화");

        SteamLobbyManager.Instance?.OnMirrorStopped();
    }
    public override void OnServerDisconnect(
        NetworkConnectionToClient conn)
    {
        bool wasInGame =
            SceneManager.GetActiveScene().name == "GameScene";

        bool wasRegisteredPlayer = conn.identity != null;

        bool shouldReturn =
            wasInGame &&
            wasRegisteredPlayer &&
            !returningToLobby &&
            !isShuttingDown;
        
        loadedPlayers.Remove(conn.connectionId);

        if (conn.identity != null)
        {
            spawnedPlayers.Remove(conn.identity.netId);
        }
        base.OnServerDisconnect(conn);

        // 퇴장 중인 연결은 제외하고 인원수 전달
        if (NetworkServer.active &&
            SceneManager.GetActiveScene().name == "MainMenu")
        {
            int count = NetworkServer.connections.Values.Count(
                c => c != null &&
                     c != conn &&
                     c.identity != null
            );

           
            LobbyPlayerCount = count;

            NetworkServer.SendToAll(
                new LobbyCountMessage { count = count }
            );

            // 호스트 UI 직접 갱신
            if (NetworkClient.active)
            {
                LobbyUI lobbyUI =
                    FindFirstObjectByType<LobbyUI>(
                        FindObjectsInactive.Include
                    );

                if (lobbyUI != null)
                    lobbyUI.RefreshLobby();
            }

            Debug.Log($"[LobbyCount] 퇴장 후 인원수: {count}/4");

        }
        if (!shouldReturn)
            return;

        Debug.Log("[Network] 게임 중 클라이언트 퇴장 감지");

        BeginGameInterruptShutdown();
    }

    [Server]
    public bool BeginGameInterruptShutdown()
    {
        if (returningToLobby || isShuttingDown)
            return false;

        bool gameFinished =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentPhase
            == GamePhase.Finished;

        returningToLobby = true;
        spawnStarted = false;
        gameStartRequested = false;

        if (startupWatchdog != null)
        {
            StopCoroutine(startupWatchdog);
            startupWatchdog = null;
        }

        if (gameFinished)
        {
            Debug.Log("[Network] 게임 정상 종료 → 전체 방 종료");
        }
        else
        {
            Debug.Log("[Network] 게임 중단 절차 시작");

            GameStateManager.Instance?.InterruptGame();
        }

        StartCoroutine(ReturnToLobbyRoutine(gameFinished));

        return true;
    }
    [Server]
    private IEnumerator ReturnToLobbyRoutine(
        bool gameFinished = false)
    {
        if (!gameFinished)
        {
            // 비정상 종료일 때만 안내 패널 3초 표시
            yield return new WaitForSecondsRealtime(3f);
        }
        else
        {
            // 정상 종료 시 다음 프레임에 종료
            yield return null;
        }

        if (!NetworkServer.active)
            yield break;

        Debug.Log("[Network] 전체 방 종료");

        SteamLobbyManager lobbyManager =
            SteamLobbyManager.Instance;

        if (lobbyManager != null)
        {
            lobbyManager.LeaveLobby();
        }
        else
        {
            StopHost();
        }
    }

}
