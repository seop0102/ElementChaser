using System.Collections;
using UnityEngine;

// 감옥의 외형, 크기, 낙하와 벽 충돌을 담당한다.
// 체력·구조·탈락 판정은 이후 별도 코드에서 처리한다.
public class PrisonCage : MonoBehaviour
{
    [Header("감옥 모델")]
    [SerializeField] private Transform visual;

    [Header("크기 계산에 사용할 감옥 본체")]
    [SerializeField] private Renderer[] sizeReferenceRenderers;

    [Header("낙하 설정")]
    [SerializeField, Min(0.1f)] private float dropHeight = 4f;

    [Header("충돌체 설정")]
    [SerializeField, Min(0.01f)] private float wallThickness = 0.08f;

    [Header("단독 테스트 설정")]
    [SerializeField]
    private Vector3 testSize =
        new Vector3(1.5f, 2.3f, 1.5f);

    [SerializeField, Min(0.1f)] private float testDropDuration = 0.6f;

    private Transform wallRoot;
    private BoxCollider[] walls;

    private Vector3 groundPosition;
    private bool initialized;
    private bool landed;

    public bool IsLanded => initialized && landed;

    // groundPoint는 감옥 바닥 중심, size는 감옥의 가로·높이·깊이다.
    public void Initialize(Vector3 groundPoint, Vector3 size)
    {
        if (visual == null)
        {
            Debug.LogError("PrisonCage: Visual을 연결해 주세요.", this);
            return;
        }

        size.x = Mathf.Max(0.5f, size.x);
        size.y = Mathf.Max(0.5f, size.y);
        size.z = Mathf.Max(0.5f, size.z);

        initialized = false;
        groundPosition = groundPoint;

        // 부모의 크기나 회전 영향을 받지 않는 독립 구조물로 사용한다.
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(groundPoint, Quaternion.identity);
        transform.localScale = Vector3.one;

        if (!FitVisual(size))
            return;

        BuildWalls(size);

        initialized = true;
        landed = false;

        // 처음에는 머리 위에 있고 충돌체는 꺼져 있다.
        SetDropProgress(0f);
    }

    private bool FitVisual(Vector3 size)
    {
        // 크기 계산에 사용할 본체가 연결되어 있는지 확인한다.
        if (sizeReferenceRenderers == null ||
            sizeReferenceRenderers.Length == 0)
        {
            Debug.LogError(
                "PrisonCage: Size Reference Renderers를 연결해 주세요.",
                this
            );
            return false;
        }

        foreach (Renderer reference in sizeReferenceRenderers)
        {
            if (reference == null ||
                !reference.transform.IsChildOf(visual))
            {
                Debug.LogError(
                    "PrisonCage: Visual 내부의 본체 Renderer를 연결해 주세요.",
                    this
                );
                return false;
            }
        }

        // 충돌은 별도로 생성하는 CollisionWalls가 담당한다.
        foreach (Collider collider in
                 visual.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        // 문을 제외한 본체 전체의 현재 크기를 구한다.
        Bounds bounds = GetBounds(sizeReferenceRenderers);

        Vector3 ratio = new Vector3(
            size.x / Mathf.Max(bounds.size.x, 0.001f),
            size.y / Mathf.Max(bounds.size.y, 0.001f),
            size.z / Mathf.Max(bounds.size.z, 0.001f)
        );

        // Visual 전체를 조절하므로 문도 함께 확대·축소된다.
        visual.localScale =
            Vector3.Scale(visual.localScale, ratio);

        // 크기가 바뀐 후 본체의 바닥 중심을 다시 계산한다.
        bounds = GetBounds(sizeReferenceRenderers);

        Vector3 bottomCenter = new Vector3(
            bounds.center.x,
            bounds.min.y,
            bounds.center.z
        );

        // 본체 바닥 중심을 감옥 부모의 위치에 정렬한다.
        visual.position += transform.position - bottomCenter;

        return true;
    }

    private Bounds GetBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private void BuildWalls(Vector3 size)
    {
        // 재초기화할 경우 기존 벽은 즉시 비활성화한 뒤 제거한다.
        if (wallRoot != null)
        {
            wallRoot.gameObject.SetActive(false);
            Destroy(wallRoot.gameObject);
        }

        wallRoot = new GameObject("CollisionWalls").transform;
        wallRoot.SetParent(transform, false);

        walls = new BoxCollider[5];

        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;
        float t = wallThickness;

        // 내부를 비워 두기 위해 네 벽과 천장을 따로 만든다.
        walls[0] = CreateWall(
            "Left", new Vector3(-x, y, 0),
            new Vector3(t, size.y, size.z));

        walls[1] = CreateWall(
            "Right", new Vector3(x, y, 0),
            new Vector3(t, size.y, size.z));

        walls[2] = CreateWall(
            "Front", new Vector3(0, y, z),
            new Vector3(size.x, size.y, t));

        walls[3] = CreateWall(
            "Back", new Vector3(0, y, -z),
            new Vector3(size.x, size.y, t));

        walls[4] = CreateWall(
            "Ceiling", new Vector3(0, size.y, 0),
            new Vector3(size.x, t, size.z));
    }

    private BoxCollider CreateWall(
        string wallName, Vector3 center, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.layer = gameObject.layer;
        wall.transform.SetParent(wallRoot, false);
        wall.transform.localPosition = center;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;
        collider.enabled = false;

        return collider;
    }

    // 0이면 낙하 시작, 1이면 착지 완료다.
    // 이후 서버 시간을 기준으로 계산한 진행도를 전달할 예정이다.
    public void SetDropProgress(float progress)
    {
        if (!initialized)
            return;

        float t = Mathf.Clamp01(progress);

        // t²를 사용해 내려올수록 빨라지는 느낌을 준다.
        float height = dropHeight * (1f - t * t);
        transform.position = groundPosition + Vector3.up * height;

        bool nextLanded = t >= 1f;

        if (landed != nextLanded)
        {
            landed = nextLanded;

            // 낙하 중 플레이어를 밀지 않도록 착지 후에만 충돌을 켠다.
            foreach (BoxCollider wall in walls)
                wall.enabled = landed;
        }
    }

    // Inspector 메뉴에서 실행하는 단독 테스트다.
    [ContextMenu("Test/Drop Cage")]
    private void TestDrop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play 모드에서 실행해 주세요.", this);
            return;
        }

        StopAllCoroutines();

        // 반복 테스트에서도 최초 바닥 위치를 유지한다.
        Vector3 point = initialized ? groundPosition : transform.position;
        StartCoroutine(TestDropRoutine(point));
    }

    private IEnumerator TestDropRoutine(Vector3 point)
    {
        Initialize(point, testSize);

        if (!initialized)
            yield break;

        float elapsed = 0f;

        while (elapsed < testDropDuration)
        {
            SetDropProgress(elapsed / testDropDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetDropProgress(1f);
    }
}