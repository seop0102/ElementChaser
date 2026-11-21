using UnityEngine;

// 투척체 착지 후 실행할 효과
public abstract class ThrowableImpactEffect : ScriptableObject
{
    public GameObject Activate(
        RaycastHit landingHit,
        WallPlacement? wallPlacement)
    {
        Vector4 clipPlane = this.GetClipPlane(landingHit.point);

        return this.Activate(
            landingHit.point,
            landingHit.normal,
            wallPlacement,
            0f,
            true,
            clipPlane);
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