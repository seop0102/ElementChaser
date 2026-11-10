using UnityEngine;

// 검증 전용: 착지 효과가 호출된 위치 표시
[CreateAssetMenu(
    fileName = "TestLandingMarkerEffect",
    menuName = "ElementChaser/Test/Landing Marker Effect")]
public sealed class TestLandingMarkerEffect : ThrowableImpactEffect
{
    [SerializeField] private GameObject _markerPrefab;

    public override void Activate(RaycastHit landingHit)
    {
        if (this._markerPrefab == null)
        {
            Debug.LogWarning("착지 표시 프리팹을 연결하세요.", this);
            return;
        }

        Vector3 position = landingHit.point + landingHit.normal * 0.15f;
        GameObject marker = Instantiate(
            this._markerPrefab, position, Quaternion.identity);

        Destroy(marker, 2f);
    }
}