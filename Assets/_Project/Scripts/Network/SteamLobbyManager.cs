
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        _mainMenuUI.ShowMainMenu();
        _mainMenuUI.SetCreateRoomInteractable(true);

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
        _currentLobbyId =
            new CSteamID(callback.m_ulSteamIDLobby);

        if (NetworkServer.active)
            return;

        string hostAddress =
            SteamMatchmaking.GetLobbyData(
                _currentLobbyId,
                HostAddressKey
            );

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            Debug.LogError("[SteamLobby] 호스트 주소 없음");
            return;
        }

        NetworkManager.singleton.networkAddress =
            hostAddress;

        NetworkManager.singleton.StartClient();

        if (_mainMenuUI != null)
            _mainMenuUI.ShowLobby();

        if (_lobbyUI != null)
            _lobbyUI.RefreshLobby();
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
        Debug.Log("[SteamLobby] 나가기 요청");

        ClearSteamLobby();

        if (NetworkServer.active)
        {
            // 호스트가 종료하면 모든 클라이언트 연결 해제
            NetworkManager.singleton.StopHost();
            Debug.Log("[SteamLobby] 호스트 종료");
        }
        else if (NetworkClient.active)
        {
            // 클라이언트는 본인만 연결 종료
            NetworkManager.singleton.StopClient();
            Debug.Log("[SteamLobby] 클라이언트 퇴장");
        }
        else if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            _mainMenuUI?.ShowMainMenu();
            _mainMenuUI?.SetCreateRoomInteractable(true);
        }
    }

    // 호스트 종료 등으로 강제로 연결이 끊긴 클라이언트용
    public void HandleDisconnected()
    {
        ClearSteamLobby();
        Debug.Log("[SteamLobby] 연결 종료 후 로비 정리");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
