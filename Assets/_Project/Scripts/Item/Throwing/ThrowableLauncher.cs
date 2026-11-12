using System;
using UnityEngine;

// 공통 투척 실행과 착지 효과 연결
public sealed class ThrowableLauncher : MonoBehaviour
{
    [SerializeField] private ThrowableSettings _settings;
    [SerializeField] private ThrowableProjectile _projectilePrefab;

    [Tooltip("벽 설치를 막는 기존 벽과 플레이어 레이어")]
    [SerializeField] private LayerMask _wallPlacementBlockingMask;

    public bool TryLaunch(
        ThrowableItemDefinition itemDefinition,
        Vector3 origin,
        Vector3 direction,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface,
        Func<RaycastHit, bool> canUseAtLanding)
    {
        if (itemDefinition == null)
        {
            throw new ArgumentNullException(nameof(itemDefinition));
        }

        if (canUseAtLanding == null)
        {
            throw new ArgumentNullException(nameof(canUseAtLanding));
        }

        if (this._settings == null || this._projectilePrefab == null)
        {
            throw new InvalidOperationException("투척 설정과 프리팹을 연결하세요.");
        }

        ThrowableImpactEffect impactEffect = itemDefinition.ImpactEffect;

        if (impactEffect == null)
        {
            throw new InvalidOperationException("아이템의 착지 효과를 연결하세요.");
        }

        WallPlacementSettings wallSettings = itemDefinition.WallSettings;

        bool TryValidateLanding(RaycastHit hit, out WallPlacement? wallPlacement)
        {
            wallPlacement = null;

            if (!canUseAtLanding(hit))
            {
                return false;
            }

            if (wallSettings == null)
            {
                return true;
            }

            if (!WallPlacement.TryCreate(
                    hit.point, direction, wallSettings, out WallPlacement placement)
                || !this.IsWallPlacementClear(placement))
            {
                return false;
            }

            wallPlacement = placement;
            return true;
        }

        bool CanUseAtHit(RaycastHit hit)
        {
            return TryValidateLanding(hit, out _);
        }

        this.GetPreview(
            origin,
            direction,
            collisionMask,
            isLandingSurface,
            CanUseAtHit,
            out bool canLaunch);

        if (!canLaunch)
        {
            return false;
        }

        ThrowableProjectile projectile = Instantiate(
            this._projectilePrefab, origin, Quaternion.identity);

        GameObject visualPrefab = itemDefinition.GetFlightVisualPrefab();

        if (visualPrefab != null)
        {
            Instantiate(visualPrefab, projectile.transform, false);
        }

        projectile.FlightEnded += (endReason, landingHit) =>
        {
            if (endReason == TrajectoryEndReason.Landed
                && TryValidateLanding(landingHit, out WallPlacement? placement))
            {
                impactEffect.Activate(landingHit, placement);
            }
        };

        projectile.Launch(
            origin,
            direction,
            this._settings,
            collisionMask,
            isLandingSurface);

        return true;
    }

    public TrajectoryResult GetPreview(
        Vector3 origin,
        Vector3 direction,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface,
        Func<RaycastHit, bool> canUseAtLanding,
        out bool canLaunch)
    {
        if (canUseAtLanding == null)
        {
            throw new ArgumentNullException(nameof(canUseAtLanding));
        }

        TrajectoryResult prediction =
            ThrowableTrajectoryPredictor.PredictTrajectory(
                origin,
                direction,
                this._settings,
                collisionMask,
                isLandingSurface);

        canLaunch = prediction.TryGetLandingHit(out RaycastHit hit)
            && canUseAtLanding(hit);

        return prediction;
    }

    public bool IsWallPlacementClear(WallPlacement placement)
    {
        return !Physics.CheckBox(
            placement.BoxCenter,
            placement.Size * 0.5f,
            placement.Rotation,
            this._wallPlacementBlockingMask,
            QueryTriggerInteraction.Ignore);
    }
}