using System;
using UnityEngine;

public enum ThrowableSimulationStatus
{
    Flying,
    Landed,
    Blocked
}

// 공통 투척의 한 단계 이동 계산
public static class ThrowableSimulation
{
    private const float SurfaceOffset = 0.001f;
    private const int MaxCollisionsPerStep = 8;

    public static ThrowableSimulationStatus SimulateStep(
        ref Vector3 position,
        ref Vector3 velocity,
        float deltaTime,
        ThrowableSettings settings,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface,
        out RaycastHit landingHit)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (isLandingSurface == null)
        {
            throw new ArgumentNullException(nameof(isLandingSurface));
        }

        if (float.IsNaN(deltaTime)
            || float.IsInfinity(deltaTime)
            || deltaTime <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        if (float.IsNaN(settings.ProjectileRadius)
            || float.IsInfinity(settings.ProjectileRadius)
            || settings.ProjectileRadius <= 0f)
        {
            throw new InvalidOperationException(
                "투척체 반지름은 0보다 커야 합니다.");
        }

        landingHit = default;

        // 초기 위치가 장애물 안이면 이동 불가
        if (Physics.CheckSphere(
            position,
            settings.ProjectileRadius,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            return ThrowableSimulationStatus.Blocked;
        }

        velocity += settings.Gravity * deltaTime;

        float remainingTime = deltaTime;

        for (int i = 0; i < MaxCollisionsPerStep; i++)
        {
            float speed = velocity.magnitude;

            if (speed <= 0.0001f)
            {
                return ThrowableSimulationStatus.Flying;
            }

            Vector3 direction = velocity / speed;
            float distance = speed * remainingTime;

            bool hasHit = Physics.SphereCast(
                position,
                settings.ProjectileRadius,
                direction,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                position += direction * distance;
                return ThrowableSimulationStatus.Flying;
            }

            // 충돌 직전 중심 위치
            position += direction * hit.distance;
            position += hit.normal * SurfaceOffset;

            // 타일 윗면 착지
            if (isLandingSurface(hit))
            {
                velocity = Vector3.zero;
                landingHit = hit;

                return ThrowableSimulationStatus.Landed;
            }

            remainingTime = Mathf.Max(
                0f,
                remainingTime - hit.distance / speed);

            // 벽 반사 및 감속
            velocity = Vector3.Reflect(velocity, hit.normal)
                * Mathf.Clamp01(settings.BounceSpeedRetention);

            if (remainingTime <= 0.000001f)
            {
                return ThrowableSimulationStatus.Flying;
            }
        }

        // 좁은 틈 등에서 반복 충돌 시 계산 중단
        return ThrowableSimulationStatus.Blocked;
    }
}