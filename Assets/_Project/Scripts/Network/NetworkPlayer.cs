
using Mirror;
using Steamworks;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSteamNameChanged))]
    private string _steamName;

    [SyncVar]
    private ulong _steamId;

    [SyncVar(hook = nameof(OnHostChanged))]
    private bool _isLobbyHost;

    public string SteamName => _steamName;
    public ulong SteamId => _steamId;
    public bool IsLobbyHost => _isLobbyHost;

    // 플레이어가 클라이언트에 생성됐을 때
    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log($"[NetworkPlayer] 생성: {netId}");

        RefreshLobby();
    }

    // 자기 자신의 플레이어가 준비됐을 때
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (!SteamManager.Initialized)
            return;

        CmdSetSteamInfo(
            SteamUser.GetSteamID().m_SteamID,
            SteamFriends.GetPersonaName()
        );
    }

    // 플레이어가 클라이언트에서 제거될 때
    public override void OnStopClient()
    {
        Debug.Log($"[NetworkPlayer] 제거: {netId}");

        LobbyUI lobbyUI =
            FindFirstObjectByType<LobbyUI>(
                FindObjectsInactive.Include
            );

        base.OnStopClient();

        if (lobbyUI != null &&
            lobbyUI.isActiveAndEnabled)
        {
            lobbyUI.RefreshAfterPlayerRemoved();
        }
    }

    [Command]
    private void CmdSetSteamInfo(
        ulong steamId,
        string steamName)
    {
        _steamId = steamId;
        _steamName = steamName;

        _isLobbyHost =
            connectionToClient != null &&
            connectionToClient == NetworkServer.localConnection;

        RpcRefreshLobby();
    }

    private void OnSteamNameChanged(
        string oldName,
        string newName)
    {
        RefreshLobby();
    }

    private void OnHostChanged(
        bool oldValue,
        bool newValue)
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
        LobbyUI lobbyUI =
            FindFirstObjectByType<LobbyUI>(
                FindObjectsInactive.Include
            );

        if (lobbyUI != null)
            lobbyUI.RefreshLobby();
    }
}
