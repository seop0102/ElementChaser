using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrajectoryEndReason
{
    Landed,
    Blocked,
    TimedOut
}

// 전체 투척 경로 예측 결과
public sealed class TrajectoryResult
{
    private readonly RaycastHit _landingHit;

    public IReadOnlyList<Vector3> Points { get; }   // 투척 경로 예측 결과 점들
    public TrajectoryEndReason EndReason { get; }   // 투척 경로 예측 종료 이유 Blocked: 막힘, Landed: 착지, TimedOut: 시간 초과

    public bool HasLanded =>
        this.EndReason == TrajectoryEndReason.Landed;

    private TrajectoryResult(
        List<Vector3> points,
        TrajectoryEndReason endReason,
        RaycastHit landingHit)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        this.Points = new List<Vector3>(points).AsReadOnly();
        this.EndReason = endReason;
        this._landingHit = landingHit;
    }

    public static TrajectoryResult CreateLanded(
        List<Vector3> points,
        RaycastHit landingHit)
    {
        return new TrajectoryResult(
            points, TrajectoryEndReason.Landed, landingHit);
    }

    public static TrajectoryResult CreateBlocked(List<Vector3> points)
    {
        return new TrajectoryResult(
            points, TrajectoryEndReason.Blocked, default);
    }

    public static TrajectoryResult CreateTimedOut(List<Vector3> points)
    {
        return new TrajectoryResult(
            points, TrajectoryEndReason.TimedOut, default);
    }

    public bool TryGetLandingHit(out RaycastHit hit)
    {
        hit = this.HasLanded ? this._landingHit : default;
        return this.HasLanded;
    }
}