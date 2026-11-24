using System.Collections;
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

        StartCoroutine(SendSteamInfoWhenReady());
    }

    
    private IEnumerator SendSteamInfoWhenReady()
    {
        float timeout = Time.realtimeSinceStartup + 45f;
        Debug.Log("[SteamInfo] Steam 정보 준비 대기 시작");
        while (Time.realtimeSinceStartup < timeout)
        {
            // 플레이어 연결이 종료됐으면 중단
            if (!isLocalPlayer || !NetworkClient.active)
                yield break;

            // Steam 초기화 확인
            if (SteamManager.Initialized)
            {
                ulong steamId =
                    SteamUser.GetSteamID().m_SteamID;

                string steamName =
                    SteamFriends.GetPersonaName();

                // 유효한 정보인지 확인
                if (steamId != 0 &&
                    !string.IsNullOrWhiteSpace(steamName))
                {
                    Debug.Log(
                        $"[SteamInfo] 전송: {steamName} ({steamId})"
                    );

                    CmdSetSteamInfo(steamId, steamName);
                    yield break;
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        Debug.LogError(
            "[SteamInfo] 45초 동안 Steam 정보를 가져오지 못했습니다."
        );
    }

    public override void OnStopClient()
    {
        Debug.Log($"[NetworkPlayer] 제거: {netId}");

        StopAllCoroutines(); // 추가

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
        if (steamId == 0 ||
            string.IsNullOrWhiteSpace(steamName) ||
            steamName.Length > 64)
        {
            Debug.LogWarning("[SteamInfo] 유효하지 않은 Steam 정보 수신");
            return;
        }

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
