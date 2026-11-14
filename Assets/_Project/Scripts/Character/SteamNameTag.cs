
using System.Collections;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

public class SteamNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform nameTagCanvas;
    [SerializeField] private SteamAvatarFace avatarFace;

    [SyncVar(hook = nameof(OnSteamIdChanged))]
    private ulong steamId;

    private Coroutine nameLoadCoroutine;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 자기 화면에서는 자기 닉네임 숨기기
        if (nameTagCanvas != null)
            nameTagCanvas.gameObject.SetActive(false);

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamNameTag] Steam 초기화 실패");
            return;
        }

        ulong mySteamId = SteamUser.GetSteamID().m_SteamID;
        CmdSetSteamId(mySteamId);
    }

    [Command]
    private void CmdSetSteamId(ulong id)
    {
        steamId = id;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        RefreshName();
        RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        if (avatarFace == null || steamId == 0)
            return;

        avatarFace.SetSteamAvatar(steamId);
    }

    private void OnSteamIdChanged(ulong oldId, ulong newId)
    {
        RefreshName();
        RefreshAvatar();
    }

    private void RefreshName()
    {
        if (nameText == null)
            return;

        if (nameLoadCoroutine != null)
            StopCoroutine(nameLoadCoroutine);

        nameLoadCoroutine = StartCoroutine(LoadName());
    }

    private IEnumerator LoadName()
    {
        nameText.text = "Loading...";

        // Steam 초기화 대기
        float timeout = 5f;

        while (!SteamManager.Initialized && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!SteamManager.Initialized || steamId == 0)
        {
            nameText.text = "Player";
            yield break;
        }

        CSteamID id = new CSteamID(steamId);

        if (id == SteamUser.GetSteamID())
        {
            nameText.text = SteamFriends.GetPersonaName();
            yield break;
        }

        // 상대 Steam 프로필 정보 요청
        SteamFriends.RequestUserInformation(id, false);

        timeout = 10f;

        while (timeout > 0f)
        {
            string nickname =
                SteamFriends.GetFriendPersonaName(id);

            if (!string.IsNullOrEmpty(nickname) &&
                nickname != "[unknown]")
            {
                nameText.text = nickname;
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return new WaitForSeconds(0.5f);
        }

        nameText.text = "Player";
    }

private void LateUpdate()
{
    if (nameTagCanvas == null || isLocalPlayer)
        return;

    Camera cam = null;

    // 현재 클라이언트가 조종하는 플레이어의 카메라 찾기
    if (NetworkClient.localPlayer != null)
    {
        cam = NetworkClient.localPlayer
            .GetComponentInChildren<Camera>(true);
    }

    if (cam == null || !cam.isActiveAndEnabled)
        return;

    // 각 플레이어의 화면에서 닉네임이 정면을 향하도록 회전
    nameTagCanvas.rotation = cam.transform.rotation;
}
}