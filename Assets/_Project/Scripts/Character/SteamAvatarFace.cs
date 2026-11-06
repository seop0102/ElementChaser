

using System.Collections;
using UnityEngine;
using Steamworks;

public class SteamAvatarFace : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer faceRenderer;

    private Material faceMaterial;
    private Texture2D avatarTexture;
    private Coroutine loadCoroutine;

    private void Awake()
    {
        if (faceRenderer == null)
            faceRenderer = GetComponent<SkinnedMeshRenderer>();

        if (faceRenderer == null)
        {
            Debug.LogError("[Avatar] Skinned Mesh Renderer 없음!");
            return;
        }

        // 캐릭터마다 독립적인 머티리얼 생성
        faceMaterial = faceRenderer.material;
    }

    public void SetSteamAvatar(ulong steamId)
    {
        if (loadCoroutine != null)
        {
            StopCoroutine(loadCoroutine);
            loadCoroutine = null;
        }

        if (steamId == 0 || faceMaterial == null)
            return;

        loadCoroutine = StartCoroutine(LoadAvatar(steamId));
    }

    private IEnumerator LoadAvatar(ulong steamId)
    {
        // Steam 초기화 대기
        float timeout = 15f;

        while (!SteamManager.Initialized && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[Avatar] Steam 초기화 실패");
            yield break;
        }

        CSteamID id = new CSteamID(steamId);

        // 상대 프로필 정보 요청
        if (id != SteamUser.GetSteamID())
            SteamFriends.RequestUserInformation(id, false);

        // 프로필 이미지가 준비될 때까지 재시도
        timeout = 15f;

        while (timeout > 0f)
        {
            int imageId = SteamFriends.GetLargeFriendAvatar(id);

            if (imageId > 0)
            {
                ApplyAvatar(imageId);
                loadCoroutine = null;
                yield break;
            }

            // 0: 이미지 없음
            // -1: 아직 이미지 다운로드 중
            if (imageId == 0)
            {
                Debug.LogWarning(
                    $"[Avatar] 프로필 이미지 없음: {steamId}"
                );
                yield break;
            }

            timeout -= 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning(
            $"[Avatar] 이미지 로딩 시간 초과: {steamId}"
        );

        loadCoroutine = null;
    }

    private void ApplyAvatar(int imageId)
    {
        if (!SteamUtils.GetImageSize(
            imageId, out uint width, out uint height))
            return;

        if (width == 0 || height == 0)
            return;

        byte[] pixels = new byte[(int)(width * height * 4)];

        if (!SteamUtils.GetImageRGBA(
            imageId, pixels, pixels.Length))
            return;

        // Steam 이미지의 상하 방향 보정
        byte[] flipped = new byte[pixels.Length];
        int rowSize = (int)width * 4;

        for (int y = 0; y < (int)height; y++)
        {
            System.Array.Copy(
                pixels, y * rowSize,
                flipped, ((int)height - 1 - y) * rowSize,
                rowSize
            );
        }

        if (avatarTexture != null)
            Destroy(avatarTexture);

        avatarTexture = new Texture2D(
            (int)width,
            (int)height,
            TextureFormat.RGBA32,
            false
        );

        avatarTexture.LoadRawTextureData(flipped);
        avatarTexture.Apply();

        faceMaterial.mainTexture = avatarTexture;

        Debug.Log("[Avatar] Steam 프로필 사진 적용 완료!");
    }

    private void OnDestroy()
    {
        if (avatarTexture != null)
            Destroy(avatarTexture);

        if (faceMaterial != null)
            Destroy(faceMaterial);
    }
}
