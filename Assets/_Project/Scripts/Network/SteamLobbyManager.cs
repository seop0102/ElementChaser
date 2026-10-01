using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    private const string HostAddressKey = "HostAddress";
    private const int MaxLobbyMembers = 4;

    [SerializeField] private MainMenuUI _mainMenuUI;
    [SerializeField] private LobbyUI _lobbyUI;
    
    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<LobbyEnter_t> _lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;

    private CSteamID _currentLobbyId;

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized.");
            return;
        }

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _gameLobbyJoinRequested =
            Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
    }

    public void CreateLobby()
    {
        _mainMenuUI.SetCreateRoomInteractable(false);
        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            MaxLobbyMembers
        );
    }

    public void InviteFriends()
    {
        if (!_currentLobbyId.IsValid())
        {
            Debug.LogWarning("Steam lobby has not been created yet.");
            return;
        }

        bool isOverlayEnabled = SteamUtils.IsOverlayEnabled();

        Debug.Log($"Steam Overlay Enabled: {isOverlayEnabled}");

        if (!isOverlayEnabled)
        {
            Debug.LogWarning("Steam Overlay is not available.");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyId);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            _mainMenuUI.SetCreateRoomInteractable(true);

            Debug.LogError($"Failed to create Steam lobby: {callback.m_eResult}");
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress = SteamUser.GetSteamID().ToString();

        SteamMatchmaking.SetLobbyData(
            _currentLobbyId,
            HostAddressKey,
            hostAddress
        );

        NetworkManager.singleton.StartHost();
        _mainMenuUI.ShowLobby();
        _lobbyUI.RefreshLobby();
        Debug.Log(
            $"Steam lobby created. Lobby ID: {_currentLobbyId}, Host: {hostAddress}"
        );
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        if (NetworkServer.active)
        {
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(
            _currentLobbyId,
            HostAddressKey
        );

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            Debug.LogError("Host Steam ID was not found in the lobby.");
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();

        _mainMenuUI.ShowLobby();
        _lobbyUI.RefreshLobby();
        
        Debug.Log($"Connecting to host: {hostAddress}");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    
    public void LeaveLobby()
    {
        if (_currentLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;
        }

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
            Debug.Log("Host closed the room.");
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
            Debug.Log("Client left the room.");
        }

        _mainMenuUI.ShowMainMenu();
    }
}