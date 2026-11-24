using UnityEngine;

// 투명화 효과와 본인용 사용 연출 설정
[CreateAssetMenu(
    fileName = "InvisibilityEffectSettings",
    menuName = "ElementChaser/Item/Invisibility/Effect Settings")]
public sealed class InvisibilityEffectSettings : ScriptableObject
{
    [Tooltip("투명화 유지 시간")]
    [Min(0.1f)]
    public float Duration = 5f;

    [Tooltip("본인에게만 표시할 사용 연출")]
    public ParticleSystem UseEffectPrefab;

    [Tooltip("사용 연출 유지 시간")]
    [Min(0.1f)]
    public float UseEffectDuration = 0.6f;
}