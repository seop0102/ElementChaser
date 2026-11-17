using Mirror;
using UnityEngine;

// 플레이어의 감금 상태와 감옥 생성을 관리한다.
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerRoleSync))]
[RequireComponent(typeof(CharacterController))]
public class PlayerPrison : NetworkBehaviour
{
    [Header("감옥 프리팹")]
    [SerializeField] private PrisonCage cagePrefab;

    [Header("감옥 크기")]
    [SerializeField, Min(1.1f)] private float widthMultiplier = 1.5f;
    [SerializeField, Min(1.1f)] private float heightMultiplier = 1.15f;

    [Header("낙하 시간")]
    [SerializeField, Min(0.1f)] private float dropDuration = 0.6f;

    // 서버가 정한 위치·크기·시간을 모든 클라이언트에 전달한다.
    [SyncVar] private Vector3 cagePosition;
    [SyncVar] private Vector3 cageSize;
    [SyncVar] private Vector3 capturedPosition;
    [SyncVar] private double dropStartTime;
    [SyncVar] private float syncedDropDuration;
    [SyncVar] private bool isJailed;

    private PlayerHealth health;
    private PlayerRoleSync role;
    private CharacterController controller;

    // 감옥 외형은 각 컴퓨터에서 하나씩 생성한다.
    private PrisonCage currentCage;

    public bool IsJailed => isJailed;

    public bool IsDropping =>
        isJailed &&
        NetworkTime.time < dropStartTime + syncedDropDuration;

    public bool IsConfigured => cagePrefab != null;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        role = GetComponent<PlayerRoleSync>();
        controller = GetComponent<CharacterController>();
    }

    [Server]
    public void ServerEnterPrison()
    {
        // 술래와 이미 갇힌 플레이어는 대상에서 제외한다.
        if (!role.IsRunner || isJailed || health.IsDead)
            return;

        if (cagePrefab == null)
        {
            Debug.LogError("PlayerPrison: Cage Prefab을 연결해 주세요.", this);
            return;
        }

        Bounds bounds = controller.bounds;

        // 플레이어의 발밑을 감옥 바닥 중심으로 사용한다.
        cagePosition = new Vector3(
            bounds.center.x,
            bounds.min.y,
            bounds.center.z
        );

        capturedPosition = transform.position;

        float width =
            Mathf.Max(bounds.size.x, bounds.size.z) * widthMultiplier;

        cageSize = new Vector3(
            width,
            bounds.size.y * heightMultiplier,
            width
        );

        syncedDropDuration = dropDuration;
        dropStartTime = NetworkTime.time;

        // 위치와 시간 설정을 마친 뒤 감금 상태를 활성화한다.
        isJailed = true;
        role.SetJailed(true);

        Debug.Log($"[Prison] {name} 감금 시작");
    }

    private void Update()
    {
        if (!isServer && !isClient)
            return;

        if (!isJailed)
            return;

        EnsureCage();

        if (currentCage == null)
            return;

        // 모든 컴퓨터가 같은 네트워크 시간을 사용한다.
        float progress = Mathf.Clamp01(
            (float)((NetworkTime.time - dropStartTime)
                    / syncedDropDuration)
        );

        currentCage.SetDropProgress(progress);
    }

    private void EnsureCage()
    {
        if (currentCage != null || cagePrefab == null)
            return;

        // 통신 지연 사이에 이동했더라도 최초 감금 위치로 맞춘다.
        // 벽을 만들기 전에 이동시켜 벽 바깥에 갇히는 상황을 방지한다.
        if (isLocalPlayer)
        {
            bool wasEnabled = controller.enabled;

            controller.enabled = false;
            transform.position = capturedPosition;
            controller.enabled = wasEnabled;
        }

        currentCage = Instantiate(cagePrefab);
        currentCage.Initialize(cagePosition, cageSize);
    }

    // 이동 목적지가 감옥 내부를 벗어나지 않도록 제한한다.
    public Vector3 ConstrainPosition(Vector3 targetPosition)
    {
        if (!isJailed)
            return targetPosition;

        EnsureCage();

        // 낙하 중에는 감금된 위치에 고정한다.
        if (IsDropping)
            return capturedPosition;

        float radius = controller.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.z)
        );

        // 벽 두께와 캐릭터 충돌체를 고려해 여유를 둔다.
        float margin = radius + controller.skinWidth + 0.05f;
        float halfX = Mathf.Max(0f, cageSize.x * 0.5f - margin);
        float halfZ = Mathf.Max(0f, cageSize.z * 0.5f - margin);

        Vector3 centerOffset =
            controller.bounds.center - transform.position;

        float centerX = cagePosition.x - centerOffset.x;
        float centerZ = cagePosition.z - centerOffset.z;

        targetPosition.x = Mathf.Clamp(
            targetPosition.x, centerX - halfX, centerX + halfX);

        targetPosition.z = Mathf.Clamp(
            targetPosition.z, centerZ - halfZ, centerZ + halfZ);

        return targetPosition;
    }

    private void OnDestroy()
    {
        // 플레이어가 제거되면 감옥도 함께 정리한다.
        if (currentCage != null)
            Destroy(currentCage.gameObject);
    }
}