using System.Collections;
using UnityEngine;

// 덩굴 벽의 충돌, 성장 연출, 수명
public sealed class VineWallInstance : MonoBehaviour
{
    [SerializeField] private BoxCollider _wallCollider;
    [SerializeField] private Transform _boxVisual;

    [Tooltip("설치 후 유지 시간")]
    [SerializeField, Min(0.1f)] private float _lifetime = 60f;

    [Tooltip("외형이 자라는 시간")]
    [SerializeField, Min(0f)] private float _growDuration = 0.35f;

    public void ApplyPlacement(WallPlacement placement)
    {
        Vector3 size = placement.Size;

        // 콜라이더는 설치 즉시 최종 크기로 적용
        this._wallCollider.center = Vector3.up * (size.y * 0.5f);
        this._wallCollider.size = size;
        this._wallCollider.enabled = true;

        this.DestroyAfterLifetime();
        this.StartCoroutine(this.GrowVisual(size));
    }

    private void DestroyAfterLifetime()
    {
        Destroy(this.gameObject, this._lifetime);
    }

    private IEnumerator GrowVisual(Vector3 size)
    {
        if (this._growDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < this._growDuration)
            {
                float progress = Mathf.Clamp01(elapsed / this._growDuration);
                this.SetVisualHeight(size, size.y * progress);

                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        this.SetVisualHeight(size, size.y);
    }

    private void SetVisualHeight(Vector3 size, float height)
    {
        this._boxVisual.localScale = new Vector3(size.x, height, size.z);
        this._boxVisual.localPosition = Vector3.up * (height * 0.5f);
    }
}