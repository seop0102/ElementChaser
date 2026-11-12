using UnityEngine;

// 착지 지점에 덩굴 벽 설치
[CreateAssetMenu(
    fileName = "VineWallImpactEffect",
    menuName = "ElementChaser/Item/Vine Wall/Impact Effect")]
public sealed class VineWallImpactEffect : ThrowableImpactEffect
{
    [SerializeField] private VineWallInstance _wallPrefab;

    public override void Activate(
        RaycastHit landingHit,
        WallPlacement? wallPlacement)
    {
        if (!wallPlacement.HasValue || this._wallPrefab == null)
        {
            Debug.LogError("덩굴 벽 배치 또는 프리팹이 없습니다.", this);
            return;
        }

        WallPlacement placement = wallPlacement.Value;
        VineWallInstance wall = Instantiate(
            this._wallPrefab, placement.GroundCenter, placement.Rotation);

        wall.ApplyPlacement(placement);
    }
}