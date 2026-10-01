using Mirror;
using Steamworks;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSteamNameChanged))]
    private string _steamName;

    [SyncVar]
    private ulong _steamId;

    [SyncVar]
    private bool _isLobbyHost;

    public string SteamName => _steamName;
    public ulong SteamId => _steamId;
    public bool IsLobbyHost => _isLobbyHost;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        CmdSetSteamInfo(
            SteamUser.GetSteamID().m_SteamID,
            SteamFriends.GetPersonaName()
        );
    }

    [Command]
    private void CmdSetSteamInfo(ulong steamId, string steamName)
    {
        _steamId = steamId;
        _steamName = steamName;
        _isLobbyHost = connectionToClient.connectionId == 0;

        RpcRefreshLobby();
    }

    private void OnSteamNameChanged(string oldName, string newName)
    {
        RefreshLobby();
    }

    [ClientRpc]
    private void RpcRefreshLobby()
    {
        RefreshLobby();
    }

    private void RefreshLobby()
    {
        LobbyUI lobbyUI = FindAnyObjectByType<LobbyUI>();

        if (lobbyUI != null)
        {
            lobbyUI.RefreshLobby();
        }
    }
}