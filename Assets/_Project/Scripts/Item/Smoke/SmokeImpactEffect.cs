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

    public override void Activate(
        RaycastHit landingHit,
        WallPlacement? wallPlacement)
    {
        if (this._smokeCloudPrefab == null || this._settings == null)
        {
            Debug.LogError("연막 프리팹과 설정을 연결하세요.", this);
            return;
        }

        Vector3 center = landingHit.point;
        GameObject smokeCloud = Instantiate(
            this._smokeCloudPrefab, center, Quaternion.identity);

        if (this.TryGetWallPlane(center, out Vector4 clipPlane))
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            ParticleSystemRenderer[] renderers =
                smokeCloud.GetComponentsInChildren<ParticleSystemRenderer>();

            foreach (ParticleSystemRenderer smokeRenderer in renderers)
            {
                smokeRenderer.GetPropertyBlock(properties);
                properties.SetVector(SmokeClipPlaneId, clipPlane);
                smokeRenderer.SetPropertyBlock(properties);
            }
        }

        Destroy(smokeCloud, this._settings.SmokeDuration);
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