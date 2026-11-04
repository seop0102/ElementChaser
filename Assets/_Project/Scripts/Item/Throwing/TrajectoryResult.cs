using System;
using System.Collections.Generic;
using UnityEngine;

// 공통 투척 경로 예측 결과
public sealed class TrajectoryResult
{
    public IReadOnlyList<Vector3> Points { get; }
    public bool HasLanded { get; }
    public Vector3 LandingPoint { get; }
    public Vector3 LandingNormal { get; }

    public TrajectoryResult(
        List<Vector3> points,   //투척 경로(점선)
        bool hasLanded,         //바닥 윗면 착지 여부
        Vector3 landingPoint,   //바닥 접촉 위치
        Vector3 landingNormal)  //바닥 접촉 법선
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        this.Points = new List<Vector3>(points).AsReadOnly();
        this.HasLanded = hasLanded;
        this.LandingPoint = landingPoint;
        this.LandingNormal = landingNormal;
    }
}