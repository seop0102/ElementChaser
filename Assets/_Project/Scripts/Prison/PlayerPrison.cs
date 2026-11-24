using System.Collections.Generic;
using Mirror;
using UnityEngine;

// 플레이어가 어느 감옥에 갇혔는지 관리한다.
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerRoleSync))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerTileController))]
public class PlayerPrison : NetworkBehaviour
{
    [Header("공동 감옥 프리팹")]
    [SerializeField] private NetworkPrison cagePrefab;

    [Header("감옥 높이")]
    [SerializeField, Min(1.1f)]
    private float heightMultiplier = 1.15f;

    [Header("낙하 시간")]
    [SerializeField, Min(0.1f)]
    private float dropDuration = 0.6f;

    [SyncVar] private Vector3 capturedPosition;

    // 0이면 자유 상태, 그 외에는 감옥의 Network ID다.
    [SyncVar(hook = nameof(OnPrisonChanged))]
    private uint prisonNetId;

    private PlayerHealth health;
    private PlayerRoleSync role;
    private CharacterController controller;
    private PlayerTileController tileController;

    public bool IsConfigured => cagePrefab != null;
    public bool IsJailed => prisonNetId != 0;

    public bool IsDropping
    {
        get
        {
            if (!IsJailed)
                return false;

            NetworkPrison prison = GetCurrentPrison();

            // 감옥 생성 메시지가 도착할 때까지도 이동을 막는다.
            return prison == null || prison.IsDropping;
        }
    }

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        role = GetComponent<PlayerRoleSync>();
        controller = GetComponent<CharacterController>();
        tileController = GetComponent<PlayerTileController>();
    }

    [Server]
    public void ServerEnterPrison()
    {
        if (!role.IsRunner || IsJailed || health.IsDead)
            return;

        if (!IsConfigured)
        {
            Debug.LogError(
                "[Prison] Cage Prefab을 연결해 주세요.",
                this
            );
            return;
        }

        Tile tile =
            tileController.GetTileAtPosition(transform.position);

        BoxCollider tileCollider = tile != null
            ? tile.GetComponent<BoxCollider>()
            : null;

        if (tileCollider == null)
        {
            Debug.LogError(
                "[Prison] 현재 타일의 BoxCollider를 찾지 못했습니다.",
                this
            );
            return;
        }

        // 같은 타일에 이미 감옥이 있다면 그 감옥을 공유한다.
        NetworkPrison prison = FindPrisonOnTile(tile);

        // 생성 순간 같은 타일에 있는 도망자들을 수집한다.
        List<PlayerPrison> prisoners = FindRunnersOnTile(tile);

        if (prison == null)
        {
            Bounds tileBounds = tileCollider.bounds;

            Vector3 position = new Vector3(
                tileBounds.center.x,
                tileBounds.max.y,
                tileBounds.center.z
            );

            // 함께 갇힐 플레이어 중 가장 큰 높이를 기준으로 한다.
            float height = controller.bounds.size.y;

            foreach (PlayerPrison prisoner in prisoners)
            {
                height = Mathf.Max(
                    height,
                    prisoner.controller.bounds.size.y
                );
            }

            Vector3 size = new Vector3(
                tileBounds.size.x,
                height * heightMultiplier,
                tileBounds.size.z
            );

            prison = Instantiate(
                cagePrefab,
                position,
                Quaternion.identity
            );

            prison.ServerConfigure(
                tile,
                position,
                size,
                dropDuration
            );

            // 서버가 감옥 하나를 생성해 모든 클라이언트에 알린다.
            NetworkServer.Spawn(prison.gameObject);
        }

        foreach (PlayerPrison prisoner in prisoners)
            prisoner.ServerJoinPrison(prison);
    }

    [Server]
    private NetworkPrison FindPrisonOnTile(Tile tile)
    {
        foreach (NetworkIdentity identity in
                 NetworkServer.spawned.Values)
        {
            if (identity == null)
                continue;

            NetworkPrison prison =
                identity.GetComponent<NetworkPrison>();

            if (prison != null && prison.ServerTile == tile)
                return prison;
        }

        return null;
    }

    [Server]
    private List<PlayerPrison> FindRunnersOnTile(Tile tile)
    {
        List<PlayerPrison> result = new List<PlayerPrison>();

        foreach (NetworkIdentity identity in
                 NetworkServer.spawned.Values)
        {
            if (identity == null)
                continue;

            PlayerPrison candidate =
                identity.GetComponent<PlayerPrison>();

            if (candidate == null ||
                candidate.IsJailed ||
                candidate.health.IsDead ||
                !candidate.role.IsRunner)
            {
                continue;
            }

            Tile candidateTile =
                candidate.tileController.GetTileAtPosition(
                    candidate.transform.position
                );

            if (candidateTile == tile)
                result.Add(candidate);
        }

        return result;
    }

    [Server]
    private void ServerJoinPrison(NetworkPrison prison)
    {
        // 체력은 변경하지 않고 감금 상태만 설정한다.
        capturedPosition = transform.position;
        prisonNetId = prison.netId;
        role.SetJailed(true);

        Debug.Log(
            $"[Prison] Player {netId} → Prison {prison.netId}"
        );
    }

    private NetworkPrison GetCurrentPrison()
    {
        if (!IsJailed)
            return null;

        NetworkIdentity identity;

        // 호스트를 포함한 클라이언트에서는 클라이언트 목록을 사용한다.
        if (isClient)
        {
            if (!NetworkClient.spawned.TryGetValue(
                    prisonNetId, out identity))
            {
                return null;
            }
        }
        else if (isServer)
        {
            if (!NetworkServer.spawned.TryGetValue(
                    prisonNetId, out identity))
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        return identity.GetComponent<NetworkPrison>();
    }

    private void OnPrisonChanged(uint oldId, uint newId)
    {
        if (newId != 0)
            ApplyCapturedPosition();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (IsJailed)
            ApplyCapturedPosition();
    }

    private void ApplyCapturedPosition()
    {
        if (!isLocalPlayer)
            return;

        // 通信 지연 사이에 움직였어도 감금된 위치로 맞춘다.
        bool wasEnabled = controller.enabled;

        controller.enabled = false;
        transform.position = capturedPosition;
        controller.enabled = wasEnabled;
    }

    public Vector3 ConstrainPosition(Vector3 targetPosition)
    {
        if (!IsJailed)
            return targetPosition;

        NetworkPrison prison = GetCurrentPrison();

        // 낙하 중이거나 감옥 정보가 아직 없으면 제자리에 고정한다.
        if (prison == null || prison.IsDropping)
            return capturedPosition;

        float radius = controller.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.z)
        );

        float margin = radius + controller.skinWidth + 0.05f;

        float halfX = Mathf.Max(
            0f,
            prison.CageSize.x * 0.5f - margin
        );

        float halfZ = Mathf.Max(
            0f,
            prison.CageSize.z * 0.5f - margin
        );

        Vector3 centerOffset =
            controller.bounds.center - transform.position;

        float centerX =
            prison.GroundPosition.x - centerOffset.x;

        float centerZ =
            prison.GroundPosition.z - centerOffset.z;

        targetPosition.x = Mathf.Clamp(
            targetPosition.x,
            centerX - halfX,
            centerX + halfX
        );

        targetPosition.z = Mathf.Clamp(
            targetPosition.z,
            centerZ - halfZ,
            centerZ + halfZ
        );

        return targetPosition;
    }

    // 전달된 감옥에 갇혀 있는 플레이어만 탈락시킨다.
    [Server]
    public void ServerEliminateFromPrison(uint expiredPrisonId)
    {
        if (!IsJailed || prisonNetId != expiredPrisonId)
            return;

        // 먼저 탈락시키고 감금 연결을 해제한다.
        health.ServerEliminate();

        prisonNetId = 0;
        role.SetJailed(false);
    }
    [Server]
    public void ServerReleaseFromPrison(uint rescuedPrisonId)
    {
        // 구조된 감옥에 갇힌 생존자만 해방한다.
        if (!IsJailed ||
            prisonNetId != rescuedPrisonId ||
            health.IsDead)
        {
            return;
        }

        health.ServerRecoverFromPrison();

        // 이동 범위 제한과 역할의 감금 상태를 함께 해제한다.
        prisonNetId = 0;
        role.SetJailed(false);

        Debug.Log($"[Prison] Player {netId} 구조 완료");
    }
}