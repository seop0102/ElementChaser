using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 덩굴 벽의 충돌, 성장 연출, 수명
public sealed class VineWallInstance : MonoBehaviour
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

    public float Lifetime => this._lifetime;

    public void ApplyPlacement(
        WallPlacement placement,
        float elapsedTime = 0f)
    {
        if (this._wallCollider == null || this._visualRoot == null
            || this._sourceModelSize.x <= 0f
            || this._sourceModelSize.y <= 0f
            || this._sourceModelSize.z <= 0f)
        {
            Debug.LogError("벽 콜라이더와 외형, 원본 크기를 확인하세요.", this);
            this.gameObject.SetActive(false);
            Destroy(this.gameObject);
            return;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);
        float remainingTime = this._lifetime - elapsedTime;

        if (remainingTime <= 0f)
        {
            this.gameObject.SetActive(false);
            Destroy(this.gameObject);
            return;
        }

        Vector3 size = placement.Size;

        // 충돌 영역은 설치 즉시 최종 크기
        this._wallCollider.center = Vector3.up * (size.y * 0.5f);
        this._wallCollider.size = size;
        this._wallCollider.enabled = true;

        this._visualRoot.localPosition = Vector3.zero;

        Destroy(this.gameObject, remainingTime);
        this.StartCoroutine(this.GrowVisual(size, elapsedTime));
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
}