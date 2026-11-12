
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
    private bool _returningToLobby = false;
    private bool _pendingHostDisconnectNotice;
    public bool ShouldShowLobby =>
        _returningToLobby || NetworkClient.active;

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

        _mainMenuUI = FindFirstObjectByType<MainMenuUI>();
        _lobbyUI = FindFirstObjectByType<LobbyUI>();

        if (_mainMenuUI == null || _lobbyUI == null)
        {
            Debug.LogError("[SteamLobby] UI 연결 실패");
            return;
        }

        if (ShouldShowLobby)
        {
            _mainMenuUI.ShowLobby();
            StartCoroutine(RefreshReturnedLobby());
        }
        else
        {
            _mainMenuUI.ShowMainMenu();
            _mainMenuUI.SetCreateRoomInteractable(true);
        }
        if (_pendingHostDisconnectNotice)
        {
            ShowMainMenuIfLoaded();
        }
        Debug.Log("[SteamLobby] MainMenu UI 재연결 완료");
    }

    public void CreateLobby()
    {
        if (!SteamManager.Initialized)
            return;

        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogWarning("[SteamLobby] 이미 연결되어 있습니다.");
            return;
        }

        if (_mainMenuUI == null)
        {
            Debug.LogError("[SteamLobby] MainMenuUI가 없습니다.");
            return;
        }

        _mainMenuUI.SetCreateRoomInteractable(false);

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
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            if (_mainMenuUI != null)
                _mainMenuUI.SetCreateRoomInteractable(true);

            Debug.LogError(
                $"Steam lobby creation failed: {callback.m_eResult}"
            );
            return;
        }

        _currentLobbyId =
            new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress =
            SteamUser.GetSteamID().ToString();

        SteamMatchmaking.SetLobbyData(
            _currentLobbyId,
            HostAddressKey,
            hostAddress
        );

        NetworkManager.singleton.StartHost();

        if (_mainMenuUI != null)
            _mainMenuUI.ShowLobby();

        if (_lobbyUI != null)
            _lobbyUI.RefreshLobby();

        Debug.Log("[SteamLobby] 방 생성 완료");
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // 자신이 방을 만든 호스트라면 클라이언트로 접속하지 않음
        if (NetworkServer.active)
            return;

        // 종료된 방이면 입장 취소
        if (SteamMatchmaking.GetLobbyData(
                _currentLobbyId, "RoomClosed") == "1")
        {
            Debug.LogWarning("[SteamLobby] 종료된 방입니다.");
            ClearSteamLobby();
            ShowMainMenuIfLoaded();
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(
            _currentLobbyId, HostAddressKey);

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            Debug.LogError("[SteamLobby] 호스트 주소 없음");
            ClearSteamLobby();
            ShowMainMenuIfLoaded();
            return;
        }

        // Steam 로비 소유권이 원래 호스트와 다르면 종료
        string lobbyOwner = SteamMatchmaking.GetLobbyOwner(
            _currentLobbyId).ToString();

        if (lobbyOwner != hostAddress)
        {
            Debug.LogWarning("[SteamLobby] 원래 호스트가 없는 방입니다.");
            ClearSteamLobby();
            ShowMainMenuIfLoaded();
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();

        _mainMenuUI?.ShowLobby();
        _lobbyUI?.RefreshLobby();
    }

    private void OnGameLobbyJoinRequested(
        GameLobbyJoinRequested_t callback
    )
    {
        SteamMatchmaking.JoinLobby(
            callback.m_steamIDLobby
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

    public void LeaveLobby()
    {
        _pendingHostDisconnectNotice = false;
        _returningToLobby = false;
        Debug.Log("[SteamLobby] 나가기 요청");

       
        bool isHost = NetworkServer.active;

        if (isHost && _currentLobbyId.IsValid())
        {
            // 호스트 종료 시 신규 참가 차단
            SteamMatchmaking.SetLobbyData(
                _currentLobbyId, "RoomClosed", "1");

            SteamMatchmaking.SetLobbyJoinable(
                _currentLobbyId, false);
        }

        ClearSteamLobby();

        if (isHost)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        ShowMainMenuIfLoaded();
    }
    public void HandleDisconnected()
    {
        _returningToLobby = false;
        _pendingHostDisconnectNotice = true;

        Debug.Log("[SteamLobby] 호스트 연결 종료 감지");

        ClearSteamLobby();
        ShowMainMenuIfLoaded();
    }
    private void ShowMainMenuIfLoaded()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            return;

        _mainMenuUI = FindFirstObjectByType<MainMenuUI>();

        if (_mainMenuUI == null)
            return;

        _mainMenuUI.ShowMainMenu();
        _mainMenuUI.SetCreateRoomInteractable(true);

        if (_pendingHostDisconnectNotice)
        {
            _pendingHostDisconnectNotice = false;
            _mainMenuUI.ShowHostDisconnectedNotice();
        }
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
}