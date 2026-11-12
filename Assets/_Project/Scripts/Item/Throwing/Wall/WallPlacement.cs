using UnityEngine;

// 착지점과 조준 방향으로 계산한 벽 배치
public readonly struct WallPlacement
{
    public Vector3 GroundCenter { get; }    //착지점ㅁ
    public Quaternion Rotation { get; }
    public Vector3 Size { get; }

    public Vector3 BoxCenter =>
        this.GroundCenter + Vector3.up * (this.Size.y * 0.5f);

    private WallPlacement(
        Vector3 groundCenter,
        Quaternion rotation,
        Vector3 size)
    {
        this.GroundCenter = groundCenter;
        this.Rotation = rotation;
        this.Size = size;
    }

    public static bool TryCreate(
        Vector3 landingPoint,
        Vector3 aimDirection,
        WallPlacementSettings settings,
        out WallPlacement placement)
    {
        placement = default;

        if (settings == null
            || !IsPositiveFinite(settings.Length)
            || !IsPositiveFinite(settings.Thickness)
            || !IsPositiveFinite(settings.Height))
        {
            return false;
        }

        Vector3 forward = new Vector3(aimDirection.x, 0f, aimDirection.z);
        float lengthSquared = forward.sqrMagnitude;

        if (!IsPositiveFinite(lengthSquared) || lengthSquared < 0.000001f)
        {
            return false;
        }

        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 size = new Vector3(
            settings.Length, settings.Height, settings.Thickness);

        placement = new WallPlacement(landingPoint, rotation, size);
        return true;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsInfinity(value);
    }
}