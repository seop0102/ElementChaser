
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum LobbyConnectionState
{
    Idle,
    Creating,
    Hosting,
    Joining,
    Connected,
    Leaving
}

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    private const string HostAddressKey = "HostAddress";
    private const int MaxLobbyMembers = 4;

    [SerializeField] private MainMenuUI _mainMenuUI;
    [SerializeField] private LobbyUI _lobbyUI;

    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<LobbyEnter_t> _lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;

    private CSteamID _currentLobbyId;
    public LobbyConnectionState CurrentState { get; private set; }
        = LobbyConnectionState.Idle;
    private bool _returningToLobby = false;
    private bool _pendingHostDisconnectNotice;
    private bool _leaveRoutineRunning;
    private bool _gameInterruptedNoticeReceived;

    private CSteamID _pendingJoinLobbyId = CSteamID.Nil;

    private Coroutine _stateTimeoutRoutine;
    public bool ShouldShowLobby =>
        CurrentState == LobbyConnectionState.Hosting ||
        CurrentState == LobbyConnectionState.Connected;
    public bool IsMirrorRunning()
    {
        NetworkManager nm = NetworkManager.singleton;

        return NetworkServer.active ||
               NetworkClient.active ||
               (nm != null &&
                nm.mode != NetworkManagerMode.Offline);
    }
    
    public void PrepareReturnToLobby()
    {
        _returningToLobby = true;
    }

    public void ClearReturnToLobby()
    {
        _returningToLobby = false;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized.");
            return;
        }

        _lobbyCreated =
            Callback<LobbyCreated_t>.Create(OnLobbyCreated);

        _lobbyEntered =
            Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        _gameLobbyJoinRequested =
            Callback<GameLobbyJoinRequested_t>.Create(
                OnGameLobbyJoinRequested
            );
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu")
            return;

        _mainMenuUI = FindFirstObjectByType<MainMenuUI>(
            FindObjectsInactive.Include
        );

        _lobbyUI = FindFirstObjectByType<LobbyUI>(
            FindObjectsInactive.Include
        );

        if (_mainMenuUI == null)
        {
            Debug.LogError("[SteamLobby] MainMenuUI 연결 실패");
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (ShouldShowLobby)
        {
            _mainMenuUI.ShowLobby();

            if (_lobbyUI != null)
                StartCoroutine(RefreshReturnedLobby());
        }
        else
        {
            _mainMenuUI.ShowMainMenu();

            _mainMenuUI.SetCreateRoomInteractable(
                CurrentState == LobbyConnectionState.Idle &&
                !IsMirrorRunning()
            );
        }

        if (_pendingHostDisconnectNotice &&
            CurrentState == LobbyConnectionState.Idle)
        {
            ShowMainMenuIfLoaded();
        }

        Debug.Log(
            $"[SteamLobby] UI 연결 완료, State={CurrentState}"
        );
    }
    
    public void OnMirrorConnected()
    {
        if (CurrentState != LobbyConnectionState.Joining)
            return;

        CurrentState = LobbyConnectionState.Connected;
        CancelStateTimeout();

        Debug.Log("[SteamLobby] Mirror 연결 완료");

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            _mainMenuUI = FindFirstObjectByType<MainMenuUI>(
                FindObjectsInactive.Include
            );

            _lobbyUI = FindFirstObjectByType<LobbyUI>(
                FindObjectsInactive.Include
            );

            _mainMenuUI?.ShowLobby();
            _lobbyUI?.RefreshLobby();
        }
    }


    public void OnMirrorStopped()
    {
        if (CurrentState == LobbyConnectionState.Idle)
            return;

        Debug.Log("[SteamLobby] Mirror 종료 감지");

        if (CurrentState != LobbyConnectionState.Leaving)
        {
            BeginLeave(
                unexpectedDisconnect: true,
                requestStop: false
            );
            return;
        }

        EnsureLeaveRoutine();
    }

    private IEnumerator FinishLeavingWhenStopped()
    {
        // BeginLeave와 Mirror 종료 콜백이 같은 프레임에
        // 실행되는 경우를 고려해 한 프레임 대기
        yield return null;

        float elapsed = 0f;
        int retryCount = 0;

        while (IsMirrorRunning())
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= 5f)
            {
                elapsed = 0f;
                retryCount++;

                NetworkManager nm = NetworkManager.singleton;

                Debug.LogWarning(
                    $"[SteamLobby] 종료 대기 {retryCount}회 " +
                    $"Server={NetworkServer.active}, " +
                    $"Client={NetworkClient.active}, " +
                    $"Mode={(nm != null ? nm.mode.ToString() : "None")}"
                );

                RequestStop();
            }

            yield return null;
        }

        ClearSteamLobby();

        _returningToLobby = false;
        _pendingJoinLobbyId = CSteamID.Nil;

        CurrentState = LobbyConnectionState.Idle;
        _leaveRoutineRunning = false;

        Debug.Log("[SteamLobby] 종료 완료 → Idle");

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            ShowMainMenuIfLoaded();
        }
    }
    
    
    public void CreateLobby()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] Steam 초기화 실패");
            return;
        }

        if (CurrentState != LobbyConnectionState.Idle)
        {
            Debug.LogWarning(
                $"[SteamLobby] 방 생성 불가: {CurrentState}"
            );
            return;
        }

        if (IsMirrorRunning())
        {
            Debug.LogWarning("[SteamLobby] 네트워크가 아직 활성화됨");
            return;
        }

        _mainMenuUI = FindFirstObjectByType<MainMenuUI>(
            FindObjectsInactive.Include
        );

        if (_mainMenuUI == null)
        {
            Debug.LogError("[SteamLobby] MainMenuUI 없음");
            return;
        }

        _gameInterruptedNoticeReceived = false;
        _pendingHostDisconnectNotice = false;

        CurrentState = LobbyConnectionState.Creating;
        _mainMenuUI.SetCreateRoomInteractable(false);

        StartStateTimeout(LobbyConnectionState.Creating);

        Debug.Log("[SteamLobby] Steam 로비 생성 요청");

        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            MaxLobbyMembers
        );
    }


    public void InviteFriends()
    {
        if (!_currentLobbyId.IsValid())
            return;

        if (!SteamUtils.IsOverlayEnabled())
            return;

        SteamFriends.ActivateGameOverlayInviteDialog(
            _currentLobbyId
        );
    }
    
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        // 방 생성 대기 상태가 아니라면 오래된 콜백
        if (CurrentState != LobbyConnectionState.Creating)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                CSteamID staleLobbyId =
                    new CSteamID(callback.m_ulSteamIDLobby);

                SteamMatchmaking.LeaveLobby(staleLobbyId);
            }

            Debug.LogWarning("[SteamLobby] 취소된 방 생성 콜백 무시");
            return;
        }

        // Steam 방 생성 실패
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            CurrentState = LobbyConnectionState.Idle;
            CancelStateTimeout();
            _mainMenuUI?.SetCreateRoomInteractable(true);

            Debug.LogError(
                $"[SteamLobby] 방 생성 실패: {callback.m_eResult}"
            );
            return;
        }

        // 정상적으로 Steam 로비 생성
        _currentLobbyId =
            new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress = SteamUser.GetSteamID().ToString();

        SteamMatchmaking.SetLobbyData(
            _currentLobbyId,
            HostAddressKey,
            hostAddress
        );

        NetworkManager.singleton.StartHost();

        if (!NetworkServer.active)
        {
            Debug.LogError("[SteamLobby] 호스트 시작 실패");

            BeginLeave(
                unexpectedDisconnect: false,
                requestStop: true
            );
            return;
        }

        CurrentState = LobbyConnectionState.Hosting;
        CancelStateTimeout();
        Debug.Log("[SteamLobby] 호스트 생성 요청 완료");

        _mainMenuUI?.ShowLobby();
        _lobbyUI?.RefreshLobby();

        Debug.Log("[SteamLobby] 방 생성 완료");
    }


    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CSteamID enteredLobbyId =
            new CSteamID(callback.m_ulSteamIDLobby);

            // 이미 호스트로 방을 생성한 상태
            if (CurrentState == LobbyConnectionState.Hosting)
            {
                if (enteredLobbyId != _currentLobbyId)
                {
                    SteamMatchmaking.LeaveLobby(enteredLobbyId);
                }

                return;
            }

            // 방 생성 중 발생한 호스트 자신의 입장 콜백
            if (CurrentState == LobbyConnectionState.Creating)
            {
                return;
            }

            if (CurrentState != LobbyConnectionState.Joining)
            {
                if (CurrentState == LobbyConnectionState.Idle ||
                    CurrentState == LobbyConnectionState.Leaving ||
                    enteredLobbyId != _currentLobbyId)
                {
                    SteamMatchmaking.LeaveLobby(enteredLobbyId);
                }

                Debug.LogWarning(
                    "[SteamLobby] 유효하지 않은 입장 콜백 무시"
                );

                return;
            }
            if (enteredLobbyId != _pendingJoinLobbyId)
            {
                Debug.LogWarning(
                    $"[SteamLobby] 오래된 로비 입장 콜백 무시: {enteredLobbyId}"
                );

                SteamMatchmaking.LeaveLobby(enteredLobbyId);
                return;
            }
        if (callback.m_EChatRoomEnterResponse !=
            (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogWarning("[SteamLobby] Steam 로비 입장 실패");
            CancelFailedJoin();
            return;
        }
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        _pendingJoinLobbyId = CSteamID.Nil;
        // 자신이 방을 만든 호스트라면 클라이언트로 접속하지 않음
        if (CurrentState == LobbyConnectionState.Creating ||
            CurrentState == LobbyConnectionState.Hosting ||
            NetworkServer.active)
        {
            return;
        }
        // 종료된 방이면 입장 취소
        if (SteamMatchmaking.GetLobbyData(
                _currentLobbyId, "RoomClosed") == "1")
        {
            Debug.LogWarning("[SteamLobby] 종료된 방입니다.");
            CancelFailedJoin();
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(
            _currentLobbyId, HostAddressKey);

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            Debug.LogError("[SteamLobby] 호스트 주소 없음");
            CancelFailedJoin();
            return;
        }

        // Steam 로비 소유권이 원래 호스트와 다르면 종료
        string lobbyOwner = SteamMatchmaking.GetLobbyOwner(
            _currentLobbyId).ToString();

        if (lobbyOwner != hostAddress)
        {
            Debug.LogWarning("[SteamLobby] 원래 호스트가 없는 방입니다.");
            CancelFailedJoin();
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();

        Debug.Log("[SteamLobby] Mirror 접속 요청");

// 로비 UI는 OnClientConnect() 성공 후 표시
    }

    
    private void OnGameLobbyJoinRequested(
        GameLobbyJoinRequested_t callback)
    {
        if (CurrentState != LobbyConnectionState.Idle)
        {
            Debug.LogWarning(
                $"[SteamLobby] 초대 수락 불가: {CurrentState}"
            );
            return;
        }

        if (IsMirrorRunning())
        {
            Debug.LogWarning(
                "[SteamLobby] 기존 네트워크 연결이 남아 있음"
            );
            return;
        }

        _pendingJoinLobbyId = callback.m_steamIDLobby;
        CurrentState = LobbyConnectionState.Joining;

        StartStateTimeout(LobbyConnectionState.Joining);

        Debug.Log(
            $"[SteamLobby] 초대 수락, Lobby={_pendingJoinLobbyId}"
        );

        SteamMatchmaking.JoinLobby(_pendingJoinLobbyId);
    }


    private void ClearSteamLobby()
    {
        if (!_currentLobbyId.IsValid())
            return;

        SteamMatchmaking.LeaveLobby(_currentLobbyId);
        _currentLobbyId = CSteamID.Nil;

        Debug.Log("[SteamLobby] Steam 로비 퇴장 완료");
    }

    private void RequestStop()
    {
        NetworkManager nm = NetworkManager.singleton;

        if (nm == null)
            return;

        if (NetworkServer.active)
        {
            (nm as ElementNetworkManager)?.BeginShutdown();
            nm.StopHost();
        }
        else if (NetworkClient.active ||
                 nm.mode != NetworkManagerMode.Offline)
        {
            nm.StopClient();
        }
    }
    private void EnsureLeaveRoutine()
    {
        if (_leaveRoutineRunning)
            return;

        _leaveRoutineRunning = true;

        StartCoroutine(FinishLeavingWhenStopped());
    }
    private void BeginLeave(
        bool unexpectedDisconnect,
        bool requestStop = true)
    {
        if (CurrentState == LobbyConnectionState.Idle)
            return;

        if (CurrentState == LobbyConnectionState.Leaving)
        {
            EnsureLeaveRoutine();
            return;
        }

        bool wasHost = NetworkServer.active;

        CurrentState = LobbyConnectionState.Leaving;

        CancelStateTimeout();

        _pendingHostDisconnectNotice =
            unexpectedDisconnect &&
            !_gameInterruptedNoticeReceived;

        _gameInterruptedNoticeReceived = false;
        _returningToLobby = false;
        _pendingJoinLobbyId = CSteamID.Nil;

        Debug.Log(
            $"[SteamLobby] 종료 시작, Host={wasHost}"
        );

        // MainMenu 씬이라면 로비 패널 즉시 닫기
        ShowMainMenuIfLoaded();

        if (wasHost && _currentLobbyId.IsValid())
        {
            SteamMatchmaking.SetLobbyData(
                _currentLobbyId,
                "RoomClosed",
                "1"
            );

            SteamMatchmaking.SetLobbyJoinable(
                _currentLobbyId,
                false
            );
        }

        ClearSteamLobby();

        // 종료 감시 먼저 시작
        EnsureLeaveRoutine();

        // 사용자의 퇴장 요청일 때만 직접 Stop 호출
        if (requestStop)
            RequestStop();
    }
    public void LeaveLobby()
    {
        BeginLeave(
            unexpectedDisconnect: false,
            requestStop: true
        );
    }
    public void HandleDisconnected()
    {
        if (CurrentState == LobbyConnectionState.Idle ||
            CurrentState == LobbyConnectionState.Leaving)
            return;

        Debug.Log("[SteamLobby] 예상하지 못한 연결 종료");

        BeginLeave(
            unexpectedDisconnect: true,
            requestStop: false
        );
    }
    private void ShowMainMenuIfLoaded()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            return;

        _mainMenuUI = FindFirstObjectByType<MainMenuUI>(
            FindObjectsInactive.Include
        );

        if (_mainMenuUI == null)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _mainMenuUI.ShowMainMenu();

        _mainMenuUI.SetCreateRoomInteractable(
            CurrentState == LobbyConnectionState.Idle &&
            !IsMirrorRunning()
        );

        if (_pendingHostDisconnectNotice &&
            CurrentState == LobbyConnectionState.Idle)
        {
            _pendingHostDisconnectNotice = false;
            _mainMenuUI.ShowHostDisconnectedNotice();
        }
    }
    public void NotifyGameInterrupted()
    {
        _gameInterruptedNoticeReceived = true;
    }

    public void LockLobby()
    {
        if (!NetworkServer.active ||
            !_currentLobbyId.IsValid())
            return;

        SteamMatchmaking.SetLobbyJoinable(
            _currentLobbyId,
            false
        );
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    private IEnumerator RefreshReturnedLobby()
    {
        // 씬 전환 직후 네트워크 오브젝트 준비 대기
        yield return null;

        if (NetworkClient.active)
        {
            _lobbyUI?.RefreshLobby();
        }
    }
    private void CancelFailedJoin()
    {
        Debug.LogWarning("[SteamLobby] 접속 실패");

        _pendingHostDisconnectNotice = false;
        _gameInterruptedNoticeReceived = false;

        BeginLeave(
            unexpectedDisconnect: false,
            requestStop: true
        );
    }
    private void StartStateTimeout(LobbyConnectionState state)
    {
        CancelStateTimeout();

        _stateTimeoutRoutine =
            StartCoroutine(StateTimeoutRoutine(state));
    }

    private void CancelStateTimeout()
    {
        if (_stateTimeoutRoutine == null)
            return;

        StopCoroutine(_stateTimeoutRoutine);
        _stateTimeoutRoutine = null;
    }

    private IEnumerator StateTimeoutRoutine(
        LobbyConnectionState state)
    {
        yield return new WaitForSecondsRealtime(15f);

        _stateTimeoutRoutine = null;

        if (CurrentState != state)
            yield break;

        Debug.LogWarning(
            $"[SteamLobby] {state} 상태 15초 타임아웃"
        );

        BeginLeave(
            unexpectedDisconnect: false,
            requestStop: true
        );
    }
    public void LeaveGameAsHost()
    {
        if (NetworkServer.active &&
            SceneManager.GetActiveScene().name == "GameScene")
        {
            ElementNetworkManager manager =
                NetworkManager.singleton as ElementNetworkManager;

            if (manager != null)
            {
                manager.BeginGameInterruptShutdown();
                return;
            }
        }

        LeaveLobby();
    }
}