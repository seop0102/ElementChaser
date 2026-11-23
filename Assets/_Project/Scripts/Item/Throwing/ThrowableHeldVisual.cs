using System;
using UnityEngine;
using UnityEngine.Rendering;

// 본인 화면과 실제 손의 아이템 외형 표시
[DisallowMultipleComponent]
public sealed class ThrowableHeldVisual : MonoBehaviour
{
    [SerializeField] private Transform _worldAnchor;
    [SerializeField] private Transform _firstPersonAnchor;
    [SerializeField] private Camera _playerCamera;

    private GameObject _worldVisual;
    private GameObject _firstPersonVisual;

    private Renderer[] _worldRenderers = Array.Empty<Renderer>();
    private Renderer[] _firstPersonRenderers = Array.Empty<Renderer>();

    private bool _isLocalPlayer;

    private void OnEnable()
    {
        Camera.onPreCull += this.UpdateCameraVisibility;
        Camera.onPreRender += this.UpdateCameraVisibility;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= this.UpdateCameraVisibility;
        Camera.onPreRender -= this.UpdateCameraVisibility;

        this.Clear();
    }

    // 장착 외형 생성
    public void Show(
        ThrowableItemDefinition definition,
        bool isLocalPlayer)
    {
        this.Clear();

        if (definition == null || definition.HeldVisualPrefab == null)
        {
            return;
        }

        if (this._worldAnchor == null
            || (isLocalPlayer
                && (this._firstPersonAnchor == null
                    || this._playerCamera == null)))
        {
            Debug.LogError("아이템 표시 위치와 카메라를 연결하세요.", this);
            return;
        }

        this._isLocalPlayer = isLocalPlayer;

        this._worldVisual = this.CreateVisual(
            definition.HeldVisualPrefab,
            definition.HeldVisualSize,
            this._worldAnchor,
            definition.HeldVisualOffset,
            definition.HeldVisualEulerAngles,
            out this._worldRenderers);

        if (isLocalPlayer)
        {
            this._firstPersonVisual = this.CreateVisual(
                definition.HeldVisualPrefab,
                definition.FirstPersonVisualSize,
                this._firstPersonAnchor,
                definition.FirstPersonVisualOffset,
                definition.FirstPersonVisualEulerAngles,
                out this._firstPersonRenderers);

            foreach (Renderer renderer in this._firstPersonRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        SetVisibility(this._worldRenderers, false);
        SetVisibility(this._firstPersonRenderers, false);
    }

    // 모델 중심과 최대 길이 보정
    private GameObject CreateVisual(
        GameObject prefab,
        float size,
        Transform anchor,
        Vector3 offset,
        Vector3 eulerAngles,
        out Renderer[] renderers)
    {
        GameObject root = new GameObject("HeldItemVisual");
        GameObject model = Instantiate(prefab, root.transform, false);

        renderers = model.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0 || !(size > 0f))
        {
            Debug.LogError("아이템 외형과 표시 크기를 확인하세요.", this);

            root.SetActive(false);
            Destroy(root);

            renderers = Array.Empty<Renderer>();
            return null;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float longestSize = Mathf.Max(
            bounds.size.x,
            Mathf.Max(bounds.size.y, bounds.size.z));

        if (!(longestSize > 0.0001f))
        {
            Debug.LogError("아이템 모델의 크기를 확인하세요.", this);

            root.SetActive(false);
            Destroy(root);

            renderers = Array.Empty<Renderer>();
            return null;
        }

        model.transform.localPosition -= bounds.center;
        root.transform.localScale = Vector3.one * (size / longestSize);

        root.transform.SetPositionAndRotation(
            anchor.position + anchor.rotation * offset,
            anchor.rotation * Quaternion.Euler(eulerAngles));

        // 손 리그의 배율과 무관하게 초기 월드 크기 유지
        root.transform.SetParent(anchor, true);

        return root;
    }

    // 카메라별 표시 구분
    private void UpdateCameraVisibility(Camera camera)
    {
        bool isLocalCamera =
            this._isLocalPlayer && camera == this._playerCamera;

        SetVisibility(this._worldRenderers, !isLocalCamera);
        SetVisibility(this._firstPersonRenderers, isLocalCamera);
    }

    private static void SetVisibility(
        Renderer[] renderers,
        bool isVisible)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.forceRenderingOff = !isVisible;
            }
        }
    }

    // 장착 외형 정리
    public void Clear()
    {
        this.DestroyVisual(ref this._worldVisual);
        this.DestroyVisual(ref this._firstPersonVisual);

        this._worldRenderers = Array.Empty<Renderer>();
        this._firstPersonRenderers = Array.Empty<Renderer>();
        this._isLocalPlayer = false;
    }

    private void DestroyVisual(ref GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        visual.SetActive(false);
        Destroy(visual);
        visual = null;
    }
}