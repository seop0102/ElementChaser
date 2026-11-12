using UnityEngine;

// 투척 궤적과 착지 범위 표시
public sealed class ThrowablePreviewRenderer : MonoBehaviour
{
    private const int CircleSegments = 48;
    private const float SurfaceOffset = 0.02f;

    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private LineRenderer _landingCircle;

    public void Show(
        TrajectoryResult prediction,
        bool canUseAtLanding,
        float landingRadius,
        WallPlacement? wallPlacement = null)
    {
        bool hasLanding = prediction.TryGetLandingHit(out RaycastHit hit);
        Color color = hasLanding && canUseAtLanding ? Color.yellow : Color.gray;

        this._trajectoryLine.enabled = prediction.Points.Count > 1;
        this._trajectoryLine.startColor = color;
        this._trajectoryLine.endColor = color;
        this._trajectoryLine.positionCount = prediction.Points.Count;

        for (int i = 0; i < prediction.Points.Count; i++)
        {
            this._trajectoryLine.SetPosition(i, prediction.Points[i]);
        }

        this._landingCircle.enabled =
            hasLanding && (wallPlacement.HasValue || landingRadius > 0f);

        if (!this._landingCircle.enabled)
        {
            return;
        }

        if (wallPlacement.HasValue)
        {
            this.DrawWallOutline(wallPlacement.Value, color);
        }
        else
        {
            this.DrawLandingCircle(hit, landingRadius, color);
        }
    }

    public void Hide()
    {
        this._trajectoryLine.enabled = false;
        this._landingCircle.enabled = false;
    }

    private void DrawLandingCircle(RaycastHit hit, float radius, Color color)
    {
        Vector3 center = hit.point + hit.normal * SurfaceOffset;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

        this._landingCircle.startColor = color;
        this._landingCircle.endColor = color;
        this._landingCircle.positionCount = CircleSegments;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = 2f * Mathf.PI * i / CircleSegments;
            Vector3 offset =
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            this._landingCircle.SetPosition(i, center + rotation * offset);
        }
    }

    private void DrawWallOutline(WallPlacement placement, Color color)
    {
        Vector3 center = placement.GroundCenter + Vector3.up * SurfaceOffset;
        Vector3 right =
            placement.Rotation * Vector3.right * (placement.Size.x * 0.5f);
        Vector3 forward =
            placement.Rotation * Vector3.forward * (placement.Size.z * 0.5f);

        this._landingCircle.startColor = color;
        this._landingCircle.endColor = color;
        this._landingCircle.positionCount = 4;

        this._landingCircle.SetPosition(0, center - right - forward);
        this._landingCircle.SetPosition(1, center + right - forward);
        this._landingCircle.SetPosition(2, center + right + forward);
        this._landingCircle.SetPosition(3, center - right + forward);
    }
}