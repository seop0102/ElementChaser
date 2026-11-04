using UnityEngine;

// 경로 예측과 실제 투척이 함께 사용하는 공통 설정
[CreateAssetMenu(
    fileName = "ThrowableSettings",
    menuName = "ElementChaser/Item/Throwing/Throwable Settings")]
public class ThrowableSettings : ScriptableObject
{
    [Header("투척 설정 — 임시값")]

    [Tooltip("초기 투척 속도")]
    [Min(0.01f)]
    public float ThrowSpeed = 10f;

    [Tooltip("투척체에 적용할 중력값")]
    public Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

    [Tooltip("충돌 계산에 사용할 투척체 반지름")]
    [Min(0.001f)]
    public float ProjectileRadius = 0.1f;

    [Tooltip("벽 반사 후 유지할 속도 비율")]
    [Range(0f, 1f)]
    public float BounceSpeedRetention = 0.5f;

    //=================================================
    [Header("경로 계산 — 임시값")]

    [Tooltip("경로 계산 시간 간격")]
    [Min(0.001f)]
    public float SimulationStep = 0.02f;

    [Tooltip("착지를 탐색할 최대 비행시간")]
    [Min(0.1f)]
    public float MaxFlightTime = 10f;
}
