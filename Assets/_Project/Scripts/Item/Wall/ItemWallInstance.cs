using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 공통 벽의 충돌, 성장 연출, 수명과 제거
public sealed class ItemWallInstance : MonoBehaviour
{
    [SerializeField] private BoxCollider _wallCollider;

    [FormerlySerializedAs("_boxVisual")]
    [SerializeField] private Transform _visualRoot;

    [Tooltip("원본 모델의 폭, 높이, 두께")]
    [SerializeField]
    private Vector3 _sourceModelSize =
        new Vector3(6f, 4f, 1.7798f);

    [Tooltip("설치 후 유지 시간")]
    [SerializeField, Min(0.1f)] private float _lifetime = 60f;

    [Tooltip("외형이 자라는 시간")]
    [SerializeField, Min(0f)] private float _growDuration = 0.35f;

    private bool _isAuthoritative;
    private bool _isRemoved;

    public float Lifetime => this._lifetime;
    public bool IsAuthoritative => this._isAuthoritative;
    public bool IsRemoved => this._isRemoved;

    public event Action<ItemWallInstance> Removed;

    // 배치와 충돌 영역 적용
    public bool ApplyPlacement(
        WallPlacement placement,
        float elapsedTime = 0f,
        bool isAuthoritative = true)
    {
        if (this._isRemoved)
        {
            return false;
        }

        if (this._wallCollider == null
            || this._visualRoot == null
            || !IsPositiveFinite(this._sourceModelSize.x)
            || !IsPositiveFinite(this._sourceModelSize.y)
            || !IsPositiveFinite(this._sourceModelSize.z)
            || !IsPositiveFinite(this._lifetime)
            || float.IsNaN(elapsedTime)
            || float.IsInfinity(elapsedTime))
        {
            Debug.LogError("벽 콜라이더와 외형, 설정값을 확인하세요.", this);
            this.Remove();
            return false;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);
        float remainingTime = this._lifetime - elapsedTime;

        if (remainingTime <= 0f)
        {
            this.Remove();
            return false;
        }

        this.CancelInvoke(nameof(Remove));
        this.StopAllCoroutines();

        this._isAuthoritative = isAuthoritative;

        Vector3 size = placement.Size;

        // 충돌 영역은 설치 즉시 최종 크기
        this._wallCollider.center = Vector3.up * (size.y * 0.5f);
        this._wallCollider.size = size;
        this._wallCollider.enabled = true;

        this._visualRoot.localPosition = Vector3.zero;

        this.Invoke(nameof(Remove), remainingTime);
        this.StartCoroutine(this.GrowVisual(size, elapsedTime));

        return true;
    }

    // 호스트 또는 오프라인 실행에서 파괴
    public bool TryDestroy()
    {
        if (!this._isAuthoritative || this._isRemoved)
        {
            return false;
        }

        this.Remove();
        return true;
    }

    // 호스트가 확정한 제거를 원격 화면에 적용
    internal void RemoveFromNetwork()
    {
        this.Remove();
    }

    // 수명 만료와 파괴의 공통 정리
    private void Remove()
    {
        if (this._isRemoved)
        {
            return;
        }

        this._isRemoved = true;

        this.CancelInvoke(nameof(Remove));
        this.StopAllCoroutines();

        if (this._wallCollider != null)
        {
            this._wallCollider.enabled = false;
        }

        this.gameObject.SetActive(false);
        Destroy(this.gameObject);

        this.NotifyRemoved();
    }

    private void OnDestroy()
    {
        if (this._isRemoved)
        {
            return;
        }

        this._isRemoved = true;
        this.NotifyRemoved();
    }

    private void NotifyRemoved()
    {
        try
        {
            this.Removed?.Invoke(this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private IEnumerator GrowVisual(Vector3 size, float elapsedTime)
    {
        if (this._growDuration > 0f)
        {
            float elapsed = elapsedTime;

            while (elapsed < this._growDuration)
            {
                float progress = Mathf.Clamp01(
                    elapsed / this._growDuration);

                this.SetVisualHeight(size, size.y * progress);

                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        this.SetVisualHeight(size, size.y);
    }

    private void SetVisualHeight(Vector3 size, float height)
    {
        this._visualRoot.localScale = new Vector3(
            size.x / this._sourceModelSize.x,
            height / this._sourceModelSize.y,
            size.z / this._sourceModelSize.z);
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsInfinity(value);
    }
}