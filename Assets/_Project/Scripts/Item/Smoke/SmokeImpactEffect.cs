using UnityEngine;

// 착지 지점에 연막 생성
[CreateAssetMenu(
    fileName = "SmokeImpactEffect",
    menuName = "ElementChaser/Item/Smoke/Impact Effect")]
public sealed class SmokeImpactEffect : ThrowableImpactEffect
{
    [SerializeField] private GameObject _smokeCloudPrefab;
    [SerializeField] private SmokeEffectSettings _settings;

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
        float remainingTime =
            this._settings.SmokeDuration - elapsedTime;

        if (remainingTime <= 0f)
        {
            return null;
        }

        GameObject smokeCloud = Instantiate(
            this._smokeCloudPrefab,
            position,
            Quaternion.identity);

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
                    && elapsedTime >=
                        main.duration + main.startDelay.constantMax)
                {
                    system.Stop(
                        false,
                        ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        Destroy(smokeCloud, remainingTime);

        return smokeCloud;
    }
}