using UnityEngine;

// 폭탄의 파괴 범위와 폭발 연출 설정 so
[CreateAssetMenu(
    fileName = "BombEffectSettings",
    menuName = "ElementChaser/Item/Bomb/Effect Settings")]
public sealed class BombEffectSettings : ScriptableObject
{
    [Tooltip("바닥 폭발의 파괴 반경")]
    [Min(0.01f)]
    public float BlastRadius = 3f;

    [Tooltip("폭발 연출 유지 시간")]
    [Min(0.1f)]
    public float EffectDuration = 1.2f;
}