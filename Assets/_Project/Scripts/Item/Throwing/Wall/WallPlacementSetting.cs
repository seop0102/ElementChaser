using UnityEngine;

// 벽 치수
[CreateAssetMenu(
    fileName = "WallPlacementSettings",
    menuName = "ElementChaser/Item/Throwing/Wall Placement Settings")]
public sealed class WallPlacementSettings : ScriptableObject
{
    // 임시값임
    [Min(0.01f)] public float Length = 6f;
    [Min(0.01f)] public float Thickness = 3f;
    [Min(0.01f)] public float Height = 4f;
}