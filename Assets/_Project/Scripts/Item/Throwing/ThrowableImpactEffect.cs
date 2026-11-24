using UnityEngine;

// 투척체 충돌 후 실행할 효과
public abstract class ThrowableImpactEffect : ScriptableObject
{
    public GameObject Activate(
        RaycastHit landingHit,
        WallPlacement? wallPlacement)
    {
        Vector4 clipPlane = this.GetClipPlane(landingHit.point);

        return this.ActivateAtHit(
            landingHit,
            wallPlacement,
            clipPlane);
    }

    // 호스트 또는 오프라인에서 실제 충돌 정보로 실행
    public virtual GameObject ActivateAtHit(
        RaycastHit hit,
        WallPlacement? wallPlacement,
        Vector4 clipPlane)
    {
        return this.Activate(
            hit.point,
            hit.normal,
            wallPlacement,
            0f,
            true,
            clipPlane);
    }

    // 아이템 효과가 사용할 원형 미리보기 반경
    public virtual float GetPreviewRadius(float defaultRadius)
    {
        return defaultRadius;
    }

    // 절단하지 않는 효과는 기본값 사용
    public virtual Vector4 GetClipPlane(Vector3 position)
    {
        return Vector4.zero;
    }

    public abstract GameObject Activate(
        Vector3 position,
        Vector3 normal,
        WallPlacement? wallPlacement,
        float elapsedTime,
        bool isAuthoritative,
        Vector4 clipPlane);
}