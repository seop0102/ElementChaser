using UnityEngine;

// 호스트 위치를 따라가는 원격 투척 외형
// 직접 물리 계산 x
public sealed class ThrowableRemoteFlight : MonoBehaviour
{
    private Vector3 _from;
    private Vector3 _target;
    private float _receivedAt;
    private float _smoothingTime;

    public void Initialize(Vector3 position, float smoothingTime)
    {
        this.transform.position = position;
        this._from = position;
        this._target = position;
        this._receivedAt = Time.unscaledTime;
        this._smoothingTime = Mathf.Max(0.001f, smoothingTime);
    }

    public void SetPosition(Vector3 position)
    {
        this._from = this.transform.position;
        this._target = position;
        this._receivedAt = Time.unscaledTime;
    }

    private void Update()
    {
        float progress =
            (Time.unscaledTime - this._receivedAt) / this._smoothingTime;

        this.transform.position = Vector3.Lerp(
            this._from,
            this._target,
            Mathf.Clamp01(progress));
    }
}