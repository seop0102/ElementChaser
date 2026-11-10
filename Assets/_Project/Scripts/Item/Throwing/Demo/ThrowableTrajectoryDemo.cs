using UnityEngine;

// 시연 전용: 공통 투척 경로를 Play Mode에서 확인
public sealed class ThrowableTrajectoryDemo : MonoBehaviour
{
    [SerializeField] private ThrowableSettings _settings;
    [SerializeField] private Transform _aim;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private LayerMask _landingMask;
    [SerializeField] private ThrowableProjectile _projectilePrefab;

    [ContextMenu("Draw Test Trajectory")]
    public void DrawTestTrajectory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play Mode에서 실행하세요.", this);
            return;
        }

        if (this._settings == null || this._aim == null)
        {
            Debug.LogWarning("투척 설정과 조준 Transform을 연결하세요.", this);
            return;
        }

        // TODO(협업): 테스트 Transform을 실제 투척 시작점·조준 방향으로 교체
        TrajectoryResult result = ThrowableTrajectoryPredictor.PredictTrajectory(
            this._aim.position,
            this._aim.forward,
            this._settings,
            this._collisionMask,
            this.IsLandingSurface);

        Color color = result.HasLanded ? Color.yellow : Color.gray;

        for (int i = 1; i < result.Points.Count; i++)
        {
            Debug.DrawLine(result.Points[i - 1], result.Points[i], color, 5f);
        }

        if (result.TryGetLandingHit(out RaycastHit hit))
        {
            Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.green, 5f);
            Debug.Log($"[투척 시연] 착지: {hit.point} / {hit.collider.name}", this);
            return;
        }

        Debug.Log($"[투척 시연] 종료: {result.EndReason}", this);
    }
    [ContextMenu("Launch Test Projectile")]
    public void LaunchTestProjectile()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play Mode에서 실행하세요.", this);
            return;
        }

        if (this._settings == null || this._aim == null
            || this._projectilePrefab == null)
        {
            Debug.LogWarning("투척 설정, 조준 Transform, 투척체 프리팹을 연결하세요.", this);
            return;
        }

        // TODO(협업): 시연 Transform을 실제 투척 시작점과 조준 방향으로 교체
        Vector3 origin = this._aim.position;
        Vector3 direction = this._aim.forward;

        // TODO(협업): 시연 충돌 마스크와 착지 판정을 실제 맵 기준으로 교체
        TrajectoryResult prediction = ThrowableTrajectoryPredictor.PredictTrajectory(
            origin,
            direction,
            this._settings,
            this._collisionMask,
            this.IsLandingSurface);

        Color color = prediction.HasLanded ? Color.yellow : Color.gray;

        for (int i = 1; i < prediction.Points.Count; i++)
        {
            Debug.DrawLine(prediction.Points[i - 1], prediction.Points[i], color, 5f);
        }

        // 시연 전용: 착지 실패 결과도 비교하기 위해 발사를 허용
        // TODO(협업: 채현): 실제 게임에서는 서버 승인 후 Launch 호출
        ThrowableProjectile projectile = Instantiate(
            this._projectilePrefab, origin, Quaternion.identity);

        projectile.FlightEnded += (endReason, landingHit) =>
            this.CompareFlightResult(prediction, endReason, landingHit);

        projectile.Launch(
            origin,
            direction,
            this._settings,
            this._collisionMask,
            this.IsLandingSurface);
    }

    private void CompareFlightResult(
        TrajectoryResult prediction,
        TrajectoryEndReason actualEndReason,
        RaycastHit actualLandingHit)
    {
        if (prediction.EndReason != actualEndReason)
        {
            Debug.LogWarning(
                $"[투척 시연] 종료 결과 불일치: 예측 {prediction.EndReason}, 실제 {actualEndReason}",
                this);
            return;
        }

        if (!prediction.TryGetLandingHit(out RaycastHit predictedLandingHit))
        {
            Debug.Log($"[투척 시연] 종료 결과 일치: {actualEndReason}", this);
            return;
        }

        float landingError = Vector3.Distance(
            predictedLandingHit.point, actualLandingHit.point);

        Debug.DrawRay(actualLandingHit.point, actualLandingHit.normal * 0.5f, Color.green, 5f);

        Debug.Log(
            $"[투척 시연] 예측 착지 {predictedLandingHit.point} / "
            + $"실제 착지 {actualLandingHit.point} / 오차 {landingError:F4}m",
            this);
    }
    // TODO(협업): 임시 레이어·법선 검사를 실제 타일 윗면 판정으로 교체
    private bool IsLandingSurface(RaycastHit hit)
    {
        int layerBit = 1 << hit.collider.gameObject.layer;
        bool isLandingLayer = (this._landingMask.value & layerBit) != 0;

        return isLandingLayer && Vector3.Dot(hit.normal, Vector3.up) >= 0.9f;
    }
}