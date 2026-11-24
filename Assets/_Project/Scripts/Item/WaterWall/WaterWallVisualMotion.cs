using UnityEngine;

// 장착, 비행, 설치 외형이 공유하는 물결 움직임
public sealed class WaterWallVisualMotion : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _renderer;

    [Tooltip("물결이 한 번 반복되는 시간")]
    [SerializeField, Min(0.1f)] private float _cycleDuration = 2.5f;

    [Tooltip("최소 파도 변형")]
    [SerializeField, Range(0f, 100f)] private float _minWeight = 0f;

    [Tooltip("최대 파도 변형")]
    [SerializeField, Range(0f, 100f)] private float _maxWeight = 100f;

    private int _waveCurlIndex = -1;

    private void Awake()
    {
        if (this._renderer == null)
        {
            this._renderer =
                this.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        Mesh mesh = this._renderer != null
            ? this._renderer.sharedMesh
            : null;

        if (mesh == null)
        {
            Debug.LogError("수벽의 Skinned Mesh Renderer를 연결하세요.", this);
            this.enabled = false;
            return;
        }

        this._waveCurlIndex = this.FindWaveCurlIndex(mesh);

        if (this._waveCurlIndex < 0)
        {
            Debug.LogError("수벽 모델에 WaveCurl이 없습니다.", this);
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (this._renderer == null)
        {
            return;
        }

        float duration = Mathf.Max(0.1f, this._cycleDuration);
        float phase = Time.time / duration * Mathf.PI * 2f;
        float progress = (1f - Mathf.Cos(phase)) * 0.5f;

        float weight = Mathf.Lerp(
            this._minWeight,
            this._maxWeight,
            progress);

        this._renderer.SetBlendShapeWeight(
            this._waveCurlIndex,
            weight);
    }

    private int FindWaveCurlIndex(Mesh mesh)
    {
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i);

            if (shapeName == "WaveCurl"
                || shapeName.EndsWith(
                    ".WaveCurl",
                    System.StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}