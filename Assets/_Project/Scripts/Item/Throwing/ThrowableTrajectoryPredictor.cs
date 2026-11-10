using System;
using System.Collections.Generic;
using UnityEngine;

// 한 단계 이동 계산을 반복해 전체 투척 경로를 예측
public static class ThrowableTrajectoryPredictor
{
    private const int MaxSimulationSteps = 10000;

    public static TrajectoryResult PredictTrajectory(
        Vector3 origin,
        Vector3 direction,
        ThrowableSettings settings,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (isLandingSurface == null)
        {
            throw new ArgumentNullException(nameof(isLandingSurface));
        }

        if (!IsFinite(origin))
        {
            throw new ArgumentException("시작 위치가 유효하지 않습니다.", nameof(origin));
        }

        if (!IsFinite(direction)
            || !IsFinite(direction.sqrMagnitude)
            || direction.sqrMagnitude <= 0.00000001f)
        {
            throw new ArgumentException("투척 방향이 유효하지 않습니다.", nameof(direction));
        }

        ValidateSettings(settings);

        double simulationStep = settings.SimulationStep;
        double maxFlightTime = settings.MaxFlightTime;
        double requiredSteps = Math.Ceiling(maxFlightTime / simulationStep);

        if (requiredSteps > MaxSimulationSteps)
        {
            throw new InvalidOperationException("투척 경로 계산 단계가 상한을 초과했습니다.");
        }

        Vector3 position = origin;
        Vector3 velocity = direction.normalized * settings.ThrowSpeed;
        var points = new List<Vector3> { origin };

        for (int stepIndex = 0; stepIndex < (int)requiredSteps; stepIndex++)
        {
            double remainingTime = maxFlightTime - stepIndex * simulationStep;

            if (remainingTime <= 0d)
            {
                break;
            }

            float deltaTime = (float)Math.Min(simulationStep, remainingTime);

            ThrowableSimulationStatus status = ThrowableSimulation.SimulateStep(
                ref position,
                ref velocity,
                deltaTime,
                settings,
                collisionMask,
                isLandingSurface,
                out RaycastHit landingHit,
                points.Add);

            points.Add(position);

            switch (status)
            {
                case ThrowableSimulationStatus.Landed:
                    return TrajectoryResult.CreateLanded(points, landingHit);

                case ThrowableSimulationStatus.Blocked:
                    return TrajectoryResult.CreateBlocked(points);
            }
        }

        return TrajectoryResult.CreateTimedOut(points);
    }

    private static void ValidateSettings(ThrowableSettings settings)
    {
        if (!IsPositiveFinite(settings.ThrowSpeed)
            || !IsPositiveFinite(settings.SimulationStep)
            || !IsPositiveFinite(settings.MaxFlightTime)
            || !IsFinite(settings.Gravity)
            || !IsFinite(settings.BounceSpeedRetention))
        {
            throw new InvalidOperationException("투척 설정값이 유효하지 않습니다.");
        }
    }

    private static bool IsPositiveFinite(float value)
    {
        return IsFinite(value) && value > 0f;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}