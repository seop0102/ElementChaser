using System;
using UnityEngine;

// 공통 투척 실행과 착지 효과 연결
public sealed class ThrowableLauncher : MonoBehaviour
{
    [SerializeField] private ThrowableSettings _settings;
    [SerializeField] private ThrowableProjectile _projectilePrefab;

    [Tooltip("벽 설치를 막는 기존 벽과 플레이어 레이어")]
    [SerializeField] private LayerMask _wallPlacementBlockingMask;

    // 실제 발사된 투척체와 아이템 정의
    public event Action<ThrowableProjectile, ThrowableItemDefinition> ProjectileLaunched;

    // 투척체, 종료 이유, 착지 정보, 효과 허용 여부, 벽 배치
    public event Action<ThrowableProjectile,TrajectoryEndReason,RaycastHit,bool,WallPlacement?,Vector4> FlightResolved;

    // 투척체와 실제 생성된 착지 효과
    public event Action<ThrowableProjectile, GameObject> ImpactActivated;

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
            bool canActivateEffect = false;
            WallPlacement? placement = null;
            Vector4 clipPlane = Vector4.zero;

            try
            {
                canActivateEffect =
                    endReason == TrajectoryEndReason.Landed
                    && TryValidateLanding(landingHit, out placement);

                if (canActivateEffect)
                {
                    clipPlane = impactEffect.GetClipPlane(landingHit.point);

                    GameObject effect = impactEffect.ActivateAtHit(
                        landingHit,
                        placement,
                        clipPlane);

                    if (effect != null)
                    {
                        this.NotifyImpactActivated(projectile, effect);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                this.NotifyFlightResolved(
                    projectile,
                    endReason,
                    landingHit,
                    canActivateEffect,
                    placement,
                    clipPlane);
            }
        };

        projectile.Launch(
            origin,
            direction,
            this._settings,
            collisionMask,
            isLandingSurface);

        this.NotifyProjectileLaunched(projectile, itemDefinition);
        return true;
    }
    private void NotifyImpactActivated(ThrowableProjectile projectile, GameObject effect)
    {
        try
        {
            this.ImpactActivated?.Invoke(projectile, effect);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void NotifyProjectileLaunched(
    ThrowableProjectile projectile,
    ThrowableItemDefinition definition)
    {
        try
        {
            this.ProjectileLaunched?.Invoke(projectile, definition);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void NotifyFlightResolved(
    ThrowableProjectile projectile,
    TrajectoryEndReason endReason,
    RaycastHit landingHit,
    bool canActivateEffect,
    WallPlacement? placement,
    Vector4 clipPlane)
    {
        try
        {
            this.FlightResolved?.Invoke(
                projectile,
                endReason,
                landingHit,
                canActivateEffect,
                placement,
                clipPlane);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
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

    // 기존 장애물과 플레이어 마스킹하여  설치 영역 검사
    public bool IsWallPlacementClear(WallPlacement placement)
    {
        Vector3 halfExtents = placement.Size * 0.5f;

        if (Physics.CheckBox(
            placement.BoxCenter,
            halfExtents,
            placement.Rotation,
            this._wallPlacementBlockingMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Collider[] overlaps = Physics.OverlapBox(
            placement.BoxCenter,
            halfExtents,
            placement.Rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap.GetComponentInParent<PlayerRoleSync>() != null)
            {
                return false;
            }
        }

        return true;
    }
}