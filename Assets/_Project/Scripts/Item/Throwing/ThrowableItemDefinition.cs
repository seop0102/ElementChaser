using UnityEngine;

// 투척 아이템의 외형과 착지 효과
[CreateAssetMenu(
    fileName = "ThrowableItemDefinition",
    menuName = "ElementChaser/Item/Throwing/Throwable Item Definition")]
public sealed class ThrowableItemDefinition : ScriptableObject
{
    public ItemType Item;

    [Tooltip("장착할 아이템 외형")]
    public GameObject HeldVisualPrefab;

    [Tooltip("비행 외형. 비우면 장착 외형 사용")]
    public GameObject FlightVisualPrefab;

    [Tooltip("착지 시 실행할 효과")]
    public ThrowableImpactEffect ImpactEffect;

    [Tooltip("벽 아이템의 배치 치수")]
    public WallPlacementSettings WallSettings;

    [Header("다른 사람에게 보이는 외형")]

    [Tooltip("외형의 최대 길이")]
    [Min(0.01f)]
    public float HeldVisualSize = 0.3f;

    [Tooltip("손 기준 위치 보정")]
    public Vector3 HeldVisualOffset;

    [Tooltip("손 기준 회전 보정")]
    public Vector3 HeldVisualEulerAngles;

    [Header("본인 화면에 보이는 외형")]

    [Tooltip("외형의 최대 길이")]
    [Min(0.01f)]
    public float FirstPersonVisualSize = 0.3f;

    [Tooltip("화면 표시 위치 기준 위치 보정")]
    public Vector3 FirstPersonVisualOffset;

    [Tooltip("화면 표시 위치 기준 회전 보정")]
    public Vector3 FirstPersonVisualEulerAngles;

    public GameObject GetFlightVisualPrefab()
    {
        return this.FlightVisualPrefab != null
            ? this.FlightVisualPrefab
            : this.HeldVisualPrefab;
    }
}