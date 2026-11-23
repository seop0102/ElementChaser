using UnityEngine;

// 착지 지점에 공통 벽 설치
[CreateAssetMenu(
    fileName = "WallImpactEffect",
    menuName = "ElementChaser/Item/Wall/Impact Effect")]
public sealed class WallImpactEffect : ThrowableImpactEffect
{
    [SerializeField] private ItemWallInstance _wallPrefab;

    public override GameObject Activate(
        Vector3 position,
        Vector3 normal,
        WallPlacement? wallPlacement,
        float elapsedTime,
        bool isAuthoritative,
        Vector4 clipPlane)
    {
        if (!wallPlacement.HasValue || this._wallPrefab == null)
        {
            Debug.LogError("벽 배치 또는 프리팹이 없습니다.", this);
            return null;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);

        if (elapsedTime >= this._wallPrefab.Lifetime)
        {
            return null;
        }

        WallPlacement placement = wallPlacement.Value;

        ItemWallInstance wall = Instantiate(
            this._wallPrefab,
            placement.GroundCenter,
            placement.Rotation);

        if (!wall.ApplyPlacement(
            placement,
            elapsedTime,
            isAuthoritative))   //true: 호스트, false: 원격에 표시
        {
            return null;
        }

        return wall.gameObject;
    }
}