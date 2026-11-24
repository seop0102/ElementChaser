using System;
using System.Linq;
using Mirror;
using UnityEngine;

// 감옥 하나의 낙하와 공통 감금 시간을 관리한다.
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(PrisonCage))]
public class NetworkPrison : NetworkBehaviour
{
    [Header("착지 후 감금 시간")]
    [SerializeField, Min(1f)] private float jailDuration = 30f;

    [SyncVar] private Vector3 groundPosition;
    [SyncVar] private Vector3 cageSize;
    [SyncVar] private double dropStartTime;
    [SyncVar] private float dropDuration;

    // 만료 시각을 동기화해 모든 컴퓨터가 같은 시간을 표시한다.
    [SyncVar] private double expireTime;

    public Tile ServerTile { get; private set; }

    private PrisonCage cage;
    private bool initialized;
    private bool expirationHandled;

    public Vector3 GroundPosition => groundPosition;
    public Vector3 CageSize => cageSize;

    public double LandTime => dropStartTime + dropDuration;

    public bool IsDropping =>
        NetworkTime.time < LandTime;

    // 낙하 중에는 감금 시간이 줄지 않는다.
    public double RemainingTime =>
        Math.Max(
            0d,
            expireTime - Math.Max(NetworkTime.time, LandTime)
        );

    private void Awake()
    {
        cage = GetComponent<PrisonCage>();
    }

    [Server]
    public void ServerConfigure(
        Tile tile,
        Vector3 position,
        Vector3 size,
        float duration)
    {
        ServerTile = tile;
        groundPosition = position;
        cageSize = size;

        dropDuration = Mathf.Max(0.1f, duration);
        dropStartTime = NetworkTime.time;

        // 착지 예정 시각부터 감금 시간을 센다.
        expireTime = LandTime + Mathf.Max(1f, jailDuration);
        expirationHandled = false;

        Debug.Log(
            $"[Prison] 감금 시간 설정: {jailDuration:0.#}초"
        );
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeCage();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        InitializeCage();
    }

    private void InitializeCage()
    {
        // 호스트에서 중복 초기화하지 않는다.
        if (initialized)
            return;

        cage.Initialize(groundPosition, cageSize);
        initialized = true;

        UpdateDrop();
    }

    private void Update()
    {
        if (initialized)
            UpdateDrop();
    }

    private void UpdateDrop()
    {
        float progress = Mathf.Clamp01(
            (float)((NetworkTime.time - dropStartTime)
                    / dropDuration)
        );

        cage.SetDropProgress(progress);
    }

    // GameStateManager가 승패 판정 전에 호출한다.
    [Server]
    public void ServerCheckExpiration(double gameEndTime)
    {
        if (expirationHandled || !initialized)
            return;

        GameStateManager game = GameStateManager.Instance;

        if (game == null ||
            game.CurrentPhase != GamePhase.Playing)
        {
            return;
        }

        // 전체 게임 시간이 끝난 뒤에는 감옥 시간 초과를 처리하지 않는다.
        if (expireTime >= gameEndTime)
            return;

        if (NetworkTime.time < expireTime)
            return;

        expirationHandled = true;

        // 탈락 처리 중 네트워크 목록이 바뀌어도 안전하도록 복사한다.
        NetworkIdentity[] identities =
            NetworkServer.spawned.Values.ToArray();

        foreach (NetworkIdentity identity in identities)
        {
            if (identity == null)
                continue;

            PlayerPrison prisoner =
                identity.GetComponent<PlayerPrison>();

            if (prisoner != null)
                prisoner.ServerEliminateFromPrison(netId);
        }

        Debug.Log($"[Prison] 감옥 {netId} 시간 초과");

        // 모든 클라이언트에서 감옥과 벽을 제거한다.
        NetworkServer.Destroy(gameObject);
    }

    // 감옥 중심이 아닌 벽까지의 거리를 구한다.
    public float GetInteractionDistance(Vector3 position)
    {
        Bounds bounds = new Bounds(
            groundPosition + Vector3.up * (cageSize.y * 0.5f),
            cageSize
        );

        return Vector3.Distance(
            position,
            bounds.ClosestPoint(position)
        );
    }

    [Server]
    public void ServerRescueAll()
    {
        GameStateManager game = GameStateManager.Instance;

        // 낙하 중이거나 시간이 끝난 감옥은 구조할 수 없다.
        if (!initialized ||
            expirationHandled ||
            IsDropping ||
            RemainingTime <= 0d ||
            game == null ||
            game.CurrentPhase != GamePhase.Playing)
        {
            return;
        }

        // 구조와 시간 초과 처리가 중복되지 않도록 막는다.
        expirationHandled = true;

        NetworkIdentity[] identities =
            NetworkServer.spawned.Values.ToArray();

        foreach (NetworkIdentity identity in identities)
        {
            if (identity == null)
                continue;

            PlayerPrison prisoner =
                identity.GetComponent<PlayerPrison>();

            if (prisoner != null)
                prisoner.ServerReleaseFromPrison(netId);
        }

        // 모든 클라이언트에서 감옥과 타이머를 제거한다.
        NetworkServer.Destroy(gameObject);
    }
}