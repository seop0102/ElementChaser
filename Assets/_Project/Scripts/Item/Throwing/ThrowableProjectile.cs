using System;
using UnityEngine;

// 공통 투척체 이동과 종료 통지
public sealed class ThrowableProjectile : MonoBehaviour
{
    private const int MaxSimulationSteps = 10000;

    private ThrowableSettings _settings;
    private LayerMask _collisionMask;
    private Func<RaycastHit, bool> _isLandingSurface;

    private Vector3 _position;
    private Vector3 _velocity;
    private double _simulationStep;
    private double _maxFlightTime;
    private double _accumulatedTime;
    private int _stepIndex;
    private bool _hasLaunched;
    private bool _isFlying;

    public event Action<TrajectoryEndReason, RaycastHit> FlightEnded;

    public void Launch(
        Vector3 origin,
        Vector3 direction,
        ThrowableSettings settings,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface)
    {
        if (this._hasLaunched)
        {
            throw new InvalidOperationException("이미 발사한 투척체입니다.");
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (isLandingSurface == null)
        {
            throw new ArgumentNullException(nameof(isLandingSurface));
        }

        float directionLengthSquared = direction.sqrMagnitude;

        if (!(directionLengthSquared > 0.00000001f)
            || float.IsInfinity(directionLengthSquared))
        {
            throw new ArgumentException("투척 방향이 유효하지 않습니다.", nameof(direction));
        }

        double simulationStep = settings.SimulationStep;
        double maxFlightTime = settings.MaxFlightTime;

        if (!(simulationStep > 0d)
            || double.IsInfinity(simulationStep)
            || !(maxFlightTime > 0d)
            || double.IsInfinity(maxFlightTime)
            || Math.Ceiling(maxFlightTime / simulationStep) > MaxSimulationSteps
            || !(settings.ThrowSpeed > 0f)
            || float.IsInfinity(settings.ThrowSpeed))
        {
            throw new InvalidOperationException("투척 설정값이 유효하지 않습니다.");
        }

        this._settings = settings;
        this._collisionMask = collisionMask;
        this._isLandingSurface = isLandingSurface;

        this._position = origin;
        this._velocity = direction.normalized * settings.ThrowSpeed;
        this._simulationStep = simulationStep;
        this._maxFlightTime = maxFlightTime;

        this.transform.position = origin;
        this._hasLaunched = true;
        this._isFlying = true;
    }

    private void FixedUpdate()
    {
        if (!this._isFlying)
        {
            return;
        }

        this._accumulatedTime += Time.fixedDeltaTime;

        // 예측기와 같은 간격으로 이동
        while (this._isFlying)
        {
            double remainingTime =
                this._maxFlightTime - this._stepIndex * this._simulationStep;

            if (remainingTime <= 0d)
            {
                this.EndFlight(TrajectoryEndReason.TimedOut, default);
                return;
            }

            double stepTime = Math.Min(this._simulationStep, remainingTime);

            if (this._accumulatedTime < stepTime)
            {
                return;
            }

            this._accumulatedTime -= stepTime;
            this._stepIndex++;

            ThrowableSimulationStatus status = ThrowableSimulation.SimulateStep(
                ref this._position,
                ref this._velocity,
                (float)stepTime,
                this._settings,
                this._collisionMask,
                this._isLandingSurface,
                out RaycastHit landingHit);

            this.transform.position = this._position;

            switch (status)
            {
                case ThrowableSimulationStatus.Landed:
                    this.EndFlight(TrajectoryEndReason.Landed, landingHit);
                    return;

                case ThrowableSimulationStatus.Blocked:
                    this.EndFlight(TrajectoryEndReason.Blocked, default);
                    return;
            }
        }
    }

    private void EndFlight(TrajectoryEndReason endReason, RaycastHit landingHit)
    {
        this._isFlying = false;
        Destroy(this.gameObject);
        this.FlightEnded?.Invoke(endReason, landingHit);
    }
}