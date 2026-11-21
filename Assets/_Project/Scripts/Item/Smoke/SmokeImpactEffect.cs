using UnityEngine;

// 착지 지점에 연막 생성
[CreateAssetMenu(
    fileName = "SmokeImpactEffect",
    menuName = "ElementChaser/Item/Smoke/Impact Effect")]
public sealed class SmokeImpactEffect : ThrowableImpactEffect
{
    private static readonly int SmokeClipPlaneId = Shader.PropertyToID("_SmokeClipPlane");

    [SerializeField] private GameObject _smokeCloudPrefab;
    [SerializeField] private SmokeEffectSettings _settings;

    [Tooltip("연막을 막는 벽 레이어")]
    [SerializeField] private LayerMask _wallMask;

    public override GameObject Activate(
    Vector3 position,
    Vector3 normal,
    WallPlacement? wallPlacement,
    float elapsedTime,
    bool isAuthoritative,
    Vector4 clipPlane)
    {
        if (this._smokeCloudPrefab == null || this._settings == null)
        {
            Debug.LogError("연막 프리팹과 설정을 연결하세요.", this);
            return null;
        }

        elapsedTime = Mathf.Max(0f, elapsedTime);
        float remainingTime = this._settings.SmokeDuration - elapsedTime;

        if (remainingTime <= 0f)
        {
            return null;
        }

        GameObject smokeCloud = Instantiate(
            this._smokeCloudPrefab, position, Quaternion.identity);

        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (ParticleSystemRenderer smokeRenderer
            in smokeCloud.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            smokeRenderer.GetPropertyBlock(properties);
            properties.SetVector(SmokeClipPlaneId, clipPlane);
            smokeRenderer.SetPropertyBlock(properties);
        }

        // 수신 지연만큼 연기 진행 상태 보정
        if (elapsedTime > 0f)
        {
            ParticleSystem[] systems =
                smokeCloud.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem system in systems)
            {
                system.Simulate(elapsedTime, false, true, true);
            }

            foreach (ParticleSystem system in systems)
            {
                system.Play(false);

                ParticleSystem.MainModule main = system.main;

                if (!main.loop
                    && elapsedTime >= main.duration + main.startDelay.constantMax)
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        Destroy(smokeCloud, remainingTime);

        return smokeCloud;
    }

    public override Vector4 GetClipPlane(Vector3 position)
    {
        this.TryGetWallPlane(position, out Vector4 clipPlane);
        return clipPlane;
    }

    // 착지점에 가장 가까운 벽 옆면 선택
    private bool TryGetWallPlane(Vector3 center, out Vector4 clipPlane)
    {
        float radius = this._settings.SmokeRadius;
        float nearestDistanceSquared = radius * radius;
        bool hasWall = false;
        clipPlane = Vector4.zero;

        Collider[] hits = Physics.OverlapSphere(
            center, radius, this._wallMask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit is not BoxCollider wall)
            {
                continue;
            }

            Vector3 surfacePoint = wall.ClosestPoint(center);
            Vector3 towardSmoke = center - surfacePoint;
            float distanceSquared = towardSmoke.sqrMagnitude;

            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            Vector3 right = wall.transform.right;
            Vector3 forward = wall.transform.forward;
            float rightDistance = Vector3.Dot(towardSmoke, right);
            float forwardDistance = Vector3.Dot(towardSmoke, forward);

            if (Mathf.Abs(rightDistance) < 0.0001f
                && Mathf.Abs(forwardDistance) < 0.0001f)
            {
                continue;
            }

            Vector3 normal = Mathf.Abs(rightDistance) >= Mathf.Abs(forwardDistance)
                ? right * Mathf.Sign(rightDistance)
                : forward * Mathf.Sign(forwardDistance);

            clipPlane = new Vector4(
                normal.x, normal.y, normal.z, -Vector3.Dot(normal, surfacePoint));

            nearestDistanceSquared = distanceSquared;
            hasWall = true;
        }

        return hasWall;
    }
}