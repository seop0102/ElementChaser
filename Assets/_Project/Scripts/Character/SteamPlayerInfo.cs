
using UnityEngine;
using Steamworks;

public class SteamPlayerInfo : MonoBehaviour
{
    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam 초기화 실패!");
            return;
        }

        CSteamID steamId = SteamUser.GetSteamID();
        string nickname = SteamFriends.GetPersonaName();

        Debug.Log($"[Steam] ID: {steamId.m_SteamID}");
        Debug.Log($"[Steam] 닉네임: {nickname}");
    }
}
