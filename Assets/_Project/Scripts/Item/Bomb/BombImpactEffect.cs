using System.Collections.Generic;
using UnityEngine;
using Mirror;

// 폭탄 직격과 바닥 폭발의 벽 파괴
[CreateAssetMenu(
    fileName = "BombImpactEffect",
    menuName = "ElementChaser/Item/Bomb/Impact Effect")]
public sealed class BombImpactEffect : ThrowableImpactEffect
{
    [SerializeField] private BombEffectSettings _settings;
    [SerializeField] private ParticleSystem _explosionPrefab;

    [Tooltip("설치 벽의 레이어")]
    [SerializeField] private LayerMask _wallMask;

    [Tooltip("감옥의 레이어")]
    [SerializeField] private LayerMask _prisonMask;

    public override float GetPreviewRadius(float defaultRadius)
    {
        return this._settings != null
            ? this._settings.BlastRadius
            : defaultRadius;
    }

    // 벽, 감옥 직격: 해당 벽 파괴,해당 감옥 구출 
    // 바닥 폭발: 범위 내 모든 벽 파괴,모든 감옥 구출
    public override GameObject ActivateAtHit(
        RaycastHit hit,
        WallPlacement? wallPlacement,
        Vector4 clipPlane)
    {
        if (hit.collider == null)
        {
            return null;
        }

        ItemWallInstance directWall = hit.collider.GetComponentInParent<ItemWallInstance>();

        NetworkPrison directPrison = hit.collider.GetComponentInParent<NetworkPrison>();

        Vector3 position = hit.point;

        GameObject explosion = this.Activate(
            position,
            hit.normal,
            wallPlacement,
            0f,
            false,
            clipPlane);

        if (explosion == null)
        {
            return null;
        }

        if (directWall != null)
        {
            // 설치 벽 직격
            directWall.TryDestroy();
        }
        else if (directPrison != null)
        {
            // 감옥 직격
            if (NetworkServer.active)
            {
                directPrison.ServerRescueAll();
            }
        }
        else
        {
            // 바닥 폭발
            this.DestroyWallsInRange(position);
            this.RescuePrisonsInRange(position);
        }

        return explosion;
    }

    // 좌표 기반 호출은 폭발 연출만 실행
    public override GameObject Activate(
        Vector3 position,
        Vector3 normal,
        WallPlacement? wallPlacement,
        float elapsedTime,
        bool isAuthoritative,
        Vector4 clipPlane)
    {
        if (!this.TryValidateSettings())
        {
            return null;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);
        float remainingTime =
            this._settings.EffectDuration - elapsedTime;

        if (remainingTime <= 0f)
        {
            return null;
        }

        ParticleSystem explosion = Instantiate(
            this._explosionPrefab,
            position + normal * 0.02f,
            Quaternion.FromToRotation(Vector3.up, normal));

        // 원격 화면은 수신 지연만큼 연출 진행
        if (elapsedTime > 0f)
        {
            explosion.Simulate(elapsedTime, true, true, true);
            explosion.Play(true);

            foreach (ParticleSystem system
                in explosion.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = system.main;

                if (!main.loop
                    && elapsedTime >=
                        main.duration + main.startDelay.constantMax)
                {
                    system.Stop(
                        false,
                        ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        Destroy(explosion.gameObject, remainingTime);

        return explosion.gameObject;
    }

    // 범위에 일부라도 걸친 설치 벽 전체 파괴
    private void DestroyWallsInRange(Vector3 position)
    {
        float radius = this._settings.BlastRadius;
        float radiusSquared = radius * radius;

        Collider[] overlaps = Physics.OverlapSphere(
            position,
            radius,
            this._wallMask,
            QueryTriggerInteraction.Ignore);

        var walls = new HashSet<ItemWallInstance>();

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null)
            {
                continue;
            }

            ItemWallInstance wall =
                overlap.GetComponentInParent<ItemWallInstance>();

            if (wall == null
                || !wall.IsAuthoritative
                || wall.IsRemoved)
            {
                continue;
            }

            Vector3 closestPoint = overlap.ClosestPoint(position);

            if ((closestPoint - position).sqrMagnitude
                <= radiusSquared)
            {
                walls.Add(wall);
            }
        }

        foreach (ItemWallInstance wall in walls)
        {
            if (wall != null)
            {
                wall.TryDestroy();
            }
        }
    }

    // 폭발 범위에 걸친 모든 감옥 해제,구출(기존 감옥쪽 api호출-r키랑 같은처리)
    private void RescuePrisonsInRange(Vector3 position)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        Collider[] overlaps = Physics.OverlapSphere(
            position,
            this._settings.BlastRadius,
            this._prisonMask,
            QueryTriggerInteraction.Ignore);

        var prisons = new HashSet<NetworkPrison>();

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null)
            {
                continue;
            }

            NetworkPrison prison =
                overlap.GetComponentInParent<NetworkPrison>();

            if (prison != null)
            {
                prisons.Add(prison);
            }
        }

        foreach (NetworkPrison prison in prisons)
        {
            if (prison != null)
            {
                prison.ServerRescueAll();
            }
        }
    }

    private bool TryValidateSettings()
    {
        if (this._settings == null
            || this._explosionPrefab == null
            || this._wallMask.value == 0
            || this._prisonMask.value == 0)
        {
            Debug.LogError("폭탄 설정과 폭발 프리팹, 벽과 감옥 레이어를 연결하세요.",this);
            return false;
        }

        if (!(this._settings.BlastRadius > 0f)
            || float.IsInfinity(this._settings.BlastRadius)
            || !(this._settings.EffectDuration > 0f)
            || float.IsInfinity(this._settings.EffectDuration))
        {
            Debug.LogError("폭탄 설정값을 확인하세요.", this);
            return false;
        }

        return true;
    }
}