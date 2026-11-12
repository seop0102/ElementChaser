
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class LobbyCharacterPreviewManager : MonoBehaviour
{
    [Header("Lobby Preview")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform[] previewPoints;
    [SerializeField] private GameObject lobbyPanel;

    private readonly Dictionary<uint, GameObject> previews = new();
    private readonly Dictionary<uint, ulong> avatarIds = new();

    private float nextRefreshTime;

    private void Update()
    {
        // 로비가 닫혀 있으면 미리보기 캐릭터 제거
        if (lobbyPanel == null || !lobbyPanel.activeInHierarchy)
        {
            ClearPreviews();
            return;
        }

        // 접속 및 퇴장 상태를 주기적으로 확인
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + 0.25f;
        RefreshPreviews();
    }

    public void RefreshPreviews()
    {
        if (characterPrefab == null || previewPoints == null)
            return;

        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None)
            .OrderBy(player => player.netId)
            .Take(previewPoints.Length)
            .ToArray();

        HashSet<uint> activeIds = new();

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayer player = players[i];
            uint netId = player.netId;

            if (netId == 0 || previewPoints[i] == null)
                continue;

            activeIds.Add(netId);

            if (!previews.TryGetValue(netId, out GameObject preview)
                || preview == null)
            {
                preview = Instantiate(
                    characterPrefab,
                    previewPoints[i]
                );

                preview.transform.localPosition = Vector3.zero;
                preview.transform.localRotation = Quaternion.identity;

                previews[netId] = preview;
            }

            // 플레이어 순서가 변경되면 위치 갱신
            if (preview.transform.parent != previewPoints[i])
            {
                preview.transform.SetParent(previewPoints[i], false);
                preview.transform.localPosition = Vector3.zero;
                preview.transform.localRotation = Quaternion.identity;
            }

            // Steam ID가 준비되거나 변경되면 얼굴 갱신
            if (player.SteamId != 0 &&
                (!avatarIds.TryGetValue(netId, out ulong oldId)
                 || oldId != player.SteamId))
            {
                SteamAvatarFace face =
                    preview.GetComponentInChildren<SteamAvatarFace>(true);

                if (face != null)
                {
                    face.SetSteamAvatar(player.SteamId);
                    avatarIds[netId] = player.SteamId;
                }
                else
                {
                    Debug.LogWarning(
                        "[LobbyPreview] SteamAvatarFace 컴포넌트 없음"
                    );
                }
            }
        }

        // 퇴장한 플레이어의 미리보기 제거
        foreach (uint id in previews.Keys.ToArray())
        {
            if (activeIds.Contains(id))
                continue;

            if (previews[id] != null)
                Destroy(previews[id]);

            previews.Remove(id);
            avatarIds.Remove(id);
        }
    }

    private void ClearPreviews()
    {
        foreach (GameObject preview in previews.Values)
        {
            if (preview != null)
                Destroy(preview);
        }

        previews.Clear();
        avatarIds.Clear();
    }

    private void OnDestroy()
    {
        ClearPreviews();
    }
}
