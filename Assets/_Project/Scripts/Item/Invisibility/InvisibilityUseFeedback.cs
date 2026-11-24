using UnityEngine;

// 사용 성공 결과를 받은 본인에게만 연출 표시
[DisallowMultipleComponent]
[RequireComponent(typeof(ThrowableItemUseController))]
public sealed class InvisibilityUseFeedback : MonoBehaviour
{
    [SerializeField] private Transform _effectAnchor;

    private ThrowableItemUseController _controller;
    private ThrowableItemNetwork _network;
    private ParticleSystem _currentEffect;

    private void Awake()
    {
        this._controller = this.GetComponent<ThrowableItemUseController>();

        this._network = this.GetComponent<ThrowableItemNetwork>();
    }

    private void OnEnable()
    {
        this._controller.ThrowRequestCompleted += this.OnUseCompleted;
    }

    private void OnDisable()
    {
        if (this._controller != null)
        {
            this._controller.ThrowRequestCompleted -= this.OnUseCompleted;
        }
        this.ClearEffect();
    }

    private void OnUseCompleted(ItemType item, bool hasUsed)
    {
        if (!hasUsed
            || item != ItemType.Invisibility
            || this._network == null
            || !this._network.isLocalPlayer)
        {
            return;
        }

        ThrowableItemDefinition definition =this._controller.FindDefinition(item);

        InvisibilityEffectSettings settings =
            definition != null
                ? definition.InvisibilitySettings
                : null;

        if (this._effectAnchor == null
            || settings == null
            || settings.UseEffectPrefab == null
            || !(settings.UseEffectDuration > 0f)
            || float.IsInfinity(settings.UseEffectDuration))
        {
            Debug.LogWarning("투명화 사용 연출과 표시 위치를 연결하세요.", this);
            return;
        }

        this.ClearEffect();

        Vector3 position =
            this._effectAnchor.position
            + this._effectAnchor.rotation
                * definition.FirstPersonVisualOffset;

        Quaternion rotation =
            this._effectAnchor.rotation
            * Quaternion.Euler(
                definition.FirstPersonVisualEulerAngles);

        this._currentEffect = Instantiate(settings.UseEffectPrefab, position,rotation);

        this._currentEffect.transform.SetParent(this._effectAnchor,true);

        this._currentEffect.Play(true);

        Destroy(this._currentEffect.gameObject, settings.UseEffectDuration);
    }

    private void ClearEffect()
    {
        if (this._currentEffect == null)
        {
            return;
        }

        this._currentEffect.gameObject.SetActive(false);
        Destroy(this._currentEffect.gameObject);
        this._currentEffect = null;
    }
}