using System;
using UnityEngine;

// 공통 투척 실행과 착지 효과 연결
public sealed class ThrowableLauncher : MonoBehaviour
{
    [SerializeField] private ThrowableSettings _settings;
    [SerializeField] private ThrowableProjectile _projectilePrefab;

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

        this.GetPreview(
            origin,
            direction,
            collisionMask,
            isLandingSurface,
            canUseAtLanding,
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
                && canUseAtLanding(landingHit))
            {
                impactEffect.Activate(landingHit);
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
}