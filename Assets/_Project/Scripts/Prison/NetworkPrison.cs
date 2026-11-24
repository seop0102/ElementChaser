using Mirror;
using UnityEngine;

// 감옥 하나의 위치·크기·낙하 시간을 동기화한다.
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(PrisonCage))]
public class NetworkPrison : NetworkBehaviour
{
    [SyncVar] private Vector3 groundPosition;
    [SyncVar] private Vector3 cageSize;
    [SyncVar] private double dropStartTime;
    [SyncVar] private float dropDuration;

    // 서버에서 같은 타일의 감옥을 찾을 때 사용한다.
    public Tile ServerTile { get; private set; }

    private PrisonCage cage;
    private bool initialized;

    public Vector3 GroundPosition => groundPosition;
    public Vector3 CageSize => cageSize;

    public bool IsDropping =>
        NetworkTime.time < dropStartTime + dropDuration;

    private void Awake()
    {
        cage = GetComponent<PrisonCage>();
    }

    // NetworkServer.Spawn을 호출하기 전에 서버가 설정한다.
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
        // 호스트에서는 서버·클라이언트 콜백이 모두 실행된다.
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
        // 늦게 받은 클라이언트도 같은 낙하 시점으로 맞춘다.
        float progress = Mathf.Clamp01(
            (float)((NetworkTime.time - dropStartTime)
                    / dropDuration)
        );

        cage.SetDropProgress(progress);
    }
}