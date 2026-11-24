
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    private readonly List<CallResult<LobbyCreated_t>>
        _createRequests = new();

    private readonly List<CallResult<LobbyEnter_t>>
        _joinRequests = new();

    private int _requestVersion = 0;

    private Callback<GameLobbyJoinRequested_t>
        _gameLobbyJoinRequested;

    private CSteamID _currentLobbyId;
    public LobbyConnectionState CurrentState { get; private set; }
        = LobbyConnectionState.Idle;
    private bool _returningToLobby = false;
    private bool _pendingHostDisconnectNotice;
    private bool _leaveRoutineRunning;
    private bool _gameInterruptedNoticeReceived;

    private CSteamID _pendingJoinLobbyId = CSteamID.Nil;

    private Coroutine _stateTimeoutRoutine;
    private Coroutine _hostLookupRoutine;
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
        StartCoroutine(InitializeSteamCallbacks());
    }
    private bool _callbacksReady = false;
    private IEnumerator InitializeSteamCallbacks()
    {
        float timeout = Time.realtimeSinceStartup + 20f;

        Debug.Log("[SteamLobby] Steam 초기화 대기 시작");

        while (!SteamManager.Initialized)
        {
            if (Time.realtimeSinceStartup >= timeout)
            {
                Debug.LogError(
                    "[SteamLobby] Steam 초기화 실패: 20초 초과"
                );
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        Debug.Log("[SteamLobby] Steam 초기화 완료");

        _gameLobbyJoinRequested =
            Callback<GameLobbyJoinRequested_t>.Create(
                OnGameLobbyJoinRequested
            );

        _callbacksReady = true;

        Debug.Log("[SteamLobby] 로비 콜백 등록 완료");
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
            bool gameFinished =
                GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentPhase
                == GamePhase.Finished;

            BeginLeave(
                unexpectedDisconnect: !gameFinished,
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

                if (retryCount <= 3)
                {
                    RequestStop();
                }
                else if (retryCount == 4)
                {
                    Debug.LogError(
                        "[SteamLobby] 네트워크 종료 실패. 게임 재실행 필요"
                    );
                }
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
        if (!SteamManager.Initialized || !_callbacksReady)
        {
            Debug.LogWarning("[SteamLobby] Steam 또는 콜백 초기화 미완료");
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

        int version = ++_requestVersion;

        CallResult<LobbyCreated_t> request = null;

        request = CallResult<LobbyCreated_t>.Create(
            (result, ioFailure) =>
            {
                _createRequests.Remove(request);

                // 이미 취소되었거나 이전 요청인 경우
                if (version != _requestVersion ||
                    CurrentState != LobbyConnectionState.Creating)
                {
                    if (!ioFailure &&
                        result.m_eResult == EResult.k_EResultOK)
                    {
                        LeaveStaleLobby(
                            new CSteamID(result.m_ulSteamIDLobby)
                        );
                    }

                    return;
                }

                // Steam 통신 실패
                if (ioFailure)
                {
                    Debug.LogWarning("[SteamLobby] 방 생성 I/O 실패");

                    BeginLeave(
                        unexpectedDisconnect: false,
                        requestStop: true
                    );

                    return;
                }

                OnLobbyCreated(result);
            });

        _createRequests.Add(request);

        request.Set(
            SteamMatchmaking.CreateLobby(
                ELobbyType.k_ELobbyTypeFriendsOnly,
                MaxLobbyMembers
            )
        );
    }


    public void InviteFriends()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamLobby] Steam 초기화 미완료");
            return;
        }

        if (!_currentLobbyId.IsValid())
        {
            Debug.LogWarning("[SteamLobby] 유효한 로비 없음");
            return;
        }

        if (!SteamUtils.IsOverlayEnabled())
        {
            Debug.LogWarning("[SteamLobby] Steam Overlay 비활성화");
            return;
        }

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

        NetworkManager manager = NetworkManager.singleton;

        if (manager == null || IsMirrorRunning())
        {
            Debug.LogError("[SteamLobby] 호스트 시작 불가");

            BeginLeave(
                unexpectedDisconnect: false,
                requestStop: true
            );

            return;
        }

        manager.StartHost();

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
        
        
        // Steam 로비 입장 성공
        Debug.Log(
            $"[SteamLobby] Steam 로비 입장 성공: {_currentLobbyId}"
        );

        // 이전 호스트 정보 대기 작업 정리
        if (_hostLookupRoutine != null)
        {
            StopCoroutine(_hostLookupRoutine);
            _hostLookupRoutine = null;
        }

        // 호스트 정보 확인 후 Mirror 연결
        _hostLookupRoutine = StartCoroutine(
            ConnectToLobbyHostWhenReady(_currentLobbyId)
        );
    }


    
private IEnumerator ConnectToLobbyHostWhenReady(CSteamID lobbyId)
{
    float timeout = Time.realtimeSinceStartup + 12f;

    Debug.Log("[SteamLobby] 호스트 정보 준비 대기 시작");
    yield return null;
    while (CurrentState == LobbyConnectionState.Joining &&
           _currentLobbyId == lobbyId &&
           Time.realtimeSinceStartup < timeout)
    {
        // 호스트가 이미 방을 종료했다면 접속 취소
        if (SteamMatchmaking.GetLobbyData(
                lobbyId, "RoomClosed") == "1")
        {
            Debug.LogWarning("[SteamLobby] 종료된 방");

            _hostLookupRoutine = null;
            CancelFailedJoin();
            yield break;
        }

        string hostAddress =
            SteamMatchmaking.GetLobbyData(
                lobbyId, HostAddressKey
            );

        CSteamID lobbyOwner =
            SteamMatchmaking.GetLobbyOwner(lobbyId);

        // 호스트 주소와 로비 소유자가 준비되었는지 확인
        if (!string.IsNullOrWhiteSpace(hostAddress) &&
            lobbyOwner.IsValid())
        {
            if (lobbyOwner.ToString() != hostAddress)
            {
                Debug.LogWarning(
                    "[SteamLobby] 호스트 주소와 로비 소유자 불일치"
                );

                _hostLookupRoutine = null;
                CancelFailedJoin();
                yield break;
            }

            NetworkManager manager = NetworkManager.singleton;

            if (manager == null)
            {
                Debug.LogError("[SteamLobby] NetworkManager 없음");

                _hostLookupRoutine = null;
                CancelFailedJoin();
                yield break;
            }

            if (IsMirrorRunning())
            {
                Debug.LogWarning(
                    "[SteamLobby] 이전 Mirror 연결이 남아 있음"
                );

                _hostLookupRoutine = null;
                CancelFailedJoin();
                yield break;
            }

            manager.networkAddress = hostAddress;

            Debug.Log(
                $"[SteamLobby] Mirror 연결 요청: {hostAddress}"
            );

            manager.StartClient();

            _hostLookupRoutine = null;
            yield break;
        }

        // Steam 로비 데이터가 아직 도착하지 않았다면 재확인
        yield return new WaitForSecondsRealtime(0.5f);
    }

    _hostLookupRoutine = null;

    if (CurrentState == LobbyConnectionState.Joining &&
        _currentLobbyId == lobbyId)
    {
        Debug.LogError(
            "[SteamLobby] 호스트 정보 준비 12초 초과"
        );

        CancelFailedJoin();
    }
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

        _mainMenuUI?.SetCreateRoomInteractable(false);

        StartStateTimeout(LobbyConnectionState.Joining);

        Debug.Log(
            $"[SteamLobby] 초대 수락, Lobby={_pendingJoinLobbyId}"
        );

        CSteamID target = _pendingJoinLobbyId;
        int version = ++_requestVersion;

        CallResult<LobbyEnter_t> request = null;

        request = CallResult<LobbyEnter_t>.Create(
            (result, ioFailure) =>
            {
                _joinRequests.Remove(request);

                // 이전 요청이거나 이미 취소된 요청
                if (version != _requestVersion ||
                    CurrentState != LobbyConnectionState.Joining)
                {
                    if (!ioFailure &&
                        result.m_EChatRoomEnterResponse ==
                        (uint)EChatRoomEnterResponse
                            .k_EChatRoomEnterResponseSuccess)
                    {
                        LeaveStaleLobby(
                            new CSteamID(result.m_ulSteamIDLobby)
                        );
                    }

                    return;
                }

                // Steam 통신 실패
                if (ioFailure)
                {
                    Debug.LogWarning("[SteamLobby] 방 참가 I/O 실패");

                    CancelFailedJoin();
                    return;
                }

                OnLobbyEntered(result);
            });

        _joinRequests.Add(request);

        request.Set(
            SteamMatchmaking.JoinLobby(target)
        );
    }

    private void LeaveStaleLobby(CSteamID lobbyId)
    {
        if (!SteamManager.Initialized)
            return;

        if (!lobbyId.IsValid())
            return;

        // 현재 사용 중인 방은 퇴장하지 않음
        if (lobbyId == _currentLobbyId)
            return;

        // 현재 접속을 시도하고 있는 방은 퇴장하지 않음
        if (lobbyId == _pendingJoinLobbyId)
            return;

        SteamMatchmaking.LeaveLobby(lobbyId);

        Debug.Log(
            $"[SteamLobby] 이전 요청 로비 정리: {lobbyId}"
        );
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

        bool wasHost =
            NetworkServer.active ||
            CurrentState == LobbyConnectionState.Hosting;

        CurrentState = LobbyConnectionState.Leaving;

        ++_requestVersion;

        CancelStateTimeout();

        if (_hostLookupRoutine != null)
        {
            StopCoroutine(_hostLookupRoutine);
            _hostLookupRoutine = null;
        }
        
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
    public void HandleDisconnected(bool gameFinished = false)
    {
        if (CurrentState == LobbyConnectionState.Idle ||
            CurrentState == LobbyConnectionState.Leaving)
            return;

        Debug.Log(gameFinished
            ? "[SteamLobby] 게임 정상 종료 후 연결 종료"
            : "[SteamLobby] 예상하지 못한 연결 종료");

        BeginLeave(
            unexpectedDisconnect: !gameFinished,
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
        if (Instance != this)
            return;

        _callbacksReady = false;

        _gameLobbyJoinRequested?.Dispose();

        foreach (var request in _createRequests)
        {
            request.Dispose();
        }

        foreach (var request in _joinRequests)
        {
            request.Dispose();
        }

        _createRequests.Clear();
        _joinRequests.Clear();

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
        float timeout =
            state == LobbyConnectionState.Joining ? 45f : 25f;

        yield return new WaitForSecondsRealtime(timeout);

        _stateTimeoutRoutine = null;

        if (CurrentState != state)
            yield break;

        Debug.LogWarning(
            $"[SteamLobby] {state} 상태 타임아웃"
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