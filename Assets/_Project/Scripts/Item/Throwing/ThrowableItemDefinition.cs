using UnityEngine;

// 투척 아이템의 외형과 착지 효과
[CreateAssetMenu(
    fileName = "ThrowableItemDefinition",
    menuName = "ElementChaser/Item/Throwing/Throwable Item Definition")]

public sealed class ThrowableItemDefinition : ScriptableObject
{
    public ItemType Item;

    [Tooltip("선택했을 때 손에 표시할 외형")]
    public GameObject HeldVisualPrefab;

    [Tooltip("비행 중 표시할 외형. 비워두면 손 외형 사용")]
    public GameObject FlightVisualPrefab;

    [Tooltip("착지 시 실행할 효과")]
    public ThrowableImpactEffect ImpactEffect;

    [Tooltip("벽 아이템의 배치 치수")]
    public WallPlacementSettings WallSettings;

    public GameObject GetFlightVisualPrefab()
    {
        return this.FlightVisualPrefab != null
            ? this.FlightVisualPrefab
            : this.HeldVisualPrefab;
    }
}