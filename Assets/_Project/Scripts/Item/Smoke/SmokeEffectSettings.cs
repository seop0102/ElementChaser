using UnityEngine;

// 착지 후 생성할 연막의 효과 설정
[CreateAssetMenu(
    fileName = "SmokeEffectSettings",
    menuName = "ElementChaser/Item/Smoke/Smoke Effect Settings")]
public class SmokeEffectSettings : ScriptableObject
{
    [Header("연막 효과 — 임시값")]

    [Tooltip("연막과 바닥 미리보기 반지름")]
    [Min(0.01f)]
    public float SmokeRadius = 3f;

    [Tooltip("착지 후 연막 지속시간")]
    [Min(0.1f)]
    public float SmokeDuration = 10f;
}
