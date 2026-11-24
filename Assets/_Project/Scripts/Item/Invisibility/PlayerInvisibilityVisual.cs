using UnityEngine;
using UnityEngine.Rendering;

// 투명화 상태에 따른 캐릭터 외형 숨김과 복구
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInvisibility))]
public sealed class PlayerInvisibilityVisual : MonoBehaviour
{
    [SerializeField] private Transform _characterVisualRoot;
    [SerializeField] private Canvas _nameTagCanvas;

    private PlayerInvisibility _invisibility;
    private Renderer[] _renderers;
    private bool[] _previousForceRenderingOff;
    private ShadowCastingMode[] _previousShadowModes;

    private bool _isHidden;
    private bool _wasNameTagEnabled;

    private void Awake()
    {
        this._invisibility = this.GetComponent<PlayerInvisibility>();

        if (this._characterVisualRoot == null
            || this._nameTagCanvas == null)
        {
            Debug.LogError("캐릭터 외형 루트와 이름표 Canvas를 연결하세요.", this);
            this.enabled = false;
            return;
        }

        this._renderers =
            this._characterVisualRoot.GetComponentsInChildren<Renderer>(true);

        this._previousForceRenderingOff =
            new bool[this._renderers.Length];

        this._previousShadowModes =
            new ShadowCastingMode[this._renderers.Length];
    }

    private void LateUpdate()
    {
        bool isInvisible =
            this._invisibility != null
            && this._invisibility.IsInvisible;

        this.SetHidden(isInvisible);
    }

    private void SetHidden(bool isHidden)
    {
        if (this._isHidden == isHidden)
        {
            return;
        }

        for (int i = 0; i < this._renderers.Length; i++)
        {
            Renderer renderer = this._renderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (isHidden)
            {
                this._previousForceRenderingOff[i] =
                    renderer.forceRenderingOff;

                this._previousShadowModes[i] =
                    renderer.shadowCastingMode;

                renderer.forceRenderingOff = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                renderer.forceRenderingOff =
                    this._previousForceRenderingOff[i];

                renderer.shadowCastingMode =
                    this._previousShadowModes[i];
            }
        }

        if (this._nameTagCanvas != null)
        {
            if (isHidden)
            {
                this._wasNameTagEnabled = this._nameTagCanvas.enabled;
                this._nameTagCanvas.enabled = false;
            }
            else
            {
                this._nameTagCanvas.enabled = this._wasNameTagEnabled;
            }
        }

        this._isHidden = isHidden;
    }

    private void OnDisable()
    {
        this.SetHidden(false);
    }
}