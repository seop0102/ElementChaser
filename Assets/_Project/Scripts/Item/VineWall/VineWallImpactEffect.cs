using UnityEngine;

// 착지 지점에 덩굴 벽 설치
[CreateAssetMenu(
    fileName = "VineWallImpactEffect",
    menuName = "ElementChaser/Item/Vine Wall/Impact Effect")]
public sealed class VineWallImpactEffect : ThrowableImpactEffect
{
    [SerializeField] private VineWallInstance _wallPrefab;

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
            Debug.LogError("덩굴 벽 배치 또는 프리팹이 없습니다.", this);
            return null;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);

        if (elapsedTime >= this._wallPrefab.Lifetime)
        {
            return null;
        }

        WallPlacement placement = wallPlacement.Value;

        VineWallInstance wall = Instantiate(
            this._wallPrefab,
            placement.GroundCenter,
            placement.Rotation);

        wall.ApplyPlacement(placement, elapsedTime);

        return wall.gameObject;
    }
}