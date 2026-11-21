using System;
using Mirror;
using UnityEngine;
using System.Collections.Generic;

// 아이템 장착과 투척 요청 처리
[RequireComponent(typeof(ThrowableItemUseController))]
[RequireComponent(typeof(PlayerRoleSync))]
public sealed class ThrowableItemNetwork : NetworkBehaviour
{
    [Tooltip("플레이어 중심에서 허용할 투척 시작점 거리")]
    [SerializeField, Min(0.1f)] private float _maxThrowOriginDistance = 3f;

    [SyncVar(hook = nameof(OnHeldItemChanged))]
    private ItemType _heldItem = ItemType.None;

    private ThrowableItemUseController _controller;
    private PlayerRoleSync _playerRole;
    private PlayerHealth _playerHealth;
    private CharacterController _characterController;

    private bool _isWaitingForThrow;
    private bool _isProcessingThrow;

    [Tooltip("투척 위치 전송 간격")]
    [SerializeField, Min(0.02f)] private float _flightSyncInterval = 0.05f;

    private uint _nextFlightId;

    private readonly Dictionary<ThrowableProjectile, ServerFlight>
        _serverFlights = new Dictionary<ThrowableProjectile, ServerFlight>();

    private readonly Dictionary<uint, ThrowableRemoteFlight>
        _remoteFlights = new Dictionary<uint, ThrowableRemoteFlight>();

    private readonly List<GameObject> _remoteEffects = new List<GameObject>();
    private readonly List<GameObject> _serverEffects = new List<GameObject>();

    private bool _hasClearedFinishedState;

    private sealed class ServerFlight   //투척채간 구별하기 위한 식별 번호
    {
        public uint Id;
        public ItemType Item;
        public double NextSendTime;
        public Action<Vector3> PositionHandler;
    }

    public bool IsNetworkActive => this.isClient || this.isServer;

    private bool CanHoldItem =>
        this._playerRole != null
        && this._playerRole.IsRunner
        && !this._playerRole.IsJailed
        && this._playerHealth != null
        && !this._playerHealth.IsDead
        && GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentPhase == GamePhase.Playing;

    private void Awake()
    {
        this._controller = GetComponent<ThrowableItemUseController>();
        this._playerRole = GetComponent<PlayerRoleSync>();
        this._playerHealth = GetComponent<PlayerHealth>();
        this._characterController = GetComponent<CharacterController>();
    }

    internal void RequestHeldItem(ItemType item)
    {
        if (!this.isLocalPlayer || !NetworkClient.ready
            || this._isWaitingForThrow)
        {
            return;
        }

        this.CmdSetHeldItem(item);
    }

    [Command]
    private void CmdSetHeldItem(ItemType item)
    {
        bool isAccepted = false;

        if (!this._isProcessingThrow)
        {
            isAccepted = item == ItemType.None;

            if (!isAccepted && this.CanHoldItem)
            {
                ThrowableItemDefinition definition =
                    this._controller.FindDefinition(item);

                isAccepted =
                    definition != null && definition.ImpactEffect != null;
            }
        }

        if (isAccepted)
        {
            this._heldItem = item;
        }

        if (this.connectionToClient != null)
        {
            this.TargetCompleteHeldItemRequest(
                this.connectionToClient,
                item,
                this._heldItem,
                isAccepted);
        }
    }

    internal void RequestThrow(Vector3 origin, Vector3 direction)
    {
        if (!this.isLocalPlayer || !NetworkClient.ready
            || this._isWaitingForThrow || !this._controller.HasSelection)
        {
            return;
        }

        this._isWaitingForThrow = true;
        this.CmdRequestThrow(origin, direction);
    }

    [Command]
    private void CmdRequestThrow(Vector3 origin, Vector3 direction)
    {
        ItemType item = this._heldItem;

        if (this._isProcessingThrow)
        {
            this.SendThrowResult(item, false);
            return;
        }

        bool hasLaunched = false;
        Func<ItemType, bool> canUseItem = null;
        Action<ItemType, bool> onUseCompleted = null;

        this._isProcessingThrow = true;

        try
        {
            if (!this._controller.TryGetInventoryHandlers(
                    out canUseItem, out onUseCompleted))
            {
                Debug.LogWarning(
                    "호스트의 해당 플레이어에 인벤토리 처리를 연결하세요.",
                    this);
                return;
            }

            if (item == ItemType.None
                || !this.CanHoldItem
                || !this.IsValidThrowAim(origin, direction))
            {
                return;
            }

            if (!canUseItem(item))
            {
                return;
            }

            hasLaunched = this._controller.TryLaunchServerItem(
                item, origin, direction.normalized);

            if (hasLaunched)
            {
                this._heldItem = ItemType.None;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            // 실제 발사 결과를 호스트의 인벤토리에 전달
            if (item != ItemType.None && onUseCompleted != null)
            {
                try
                {
                    onUseCompleted(item, hasLaunched);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            this._isProcessingThrow = false;
            this.SendThrowResult(item, hasLaunched);
        }
    }

    private bool IsValidThrowAim(Vector3 origin, Vector3 direction)
    {
        if (!IsFinite(origin) || !IsFinite(direction))
        {
            return false;
        }

        float directionLengthSquared = direction.sqrMagnitude;

        if (!(directionLengthSquared > 0.00000001f)
            || float.IsInfinity(directionLengthSquared))
        {
            return false;
        }

        Vector3 referencePosition = this._characterController != null
            ? this._characterController.bounds.center
            : this.transform.position;

        float maxDistance = this._maxThrowOriginDistance;

        if (!(maxDistance > 0f) || float.IsInfinity(maxDistance))
        {
            return false;
        }

        if ((origin - referencePosition).sqrMagnitude
            > maxDistance * maxDistance)
        {
            return false;
        }

        // 몸과 시작점 사이의 벽을 넘어 생성하지 않도록 검사
        return !Physics.Linecast(
            referencePosition,
            origin,
            this._controller.ThrowCollisionMask,
            QueryTriggerInteraction.Ignore);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    [Server]
    private void SendThrowResult(ItemType item, bool hasLaunched)
    {
        if (this.connectionToClient != null)
        {
            this.TargetCompleteThrow(
                this.connectionToClient,
                item,
                hasLaunched,
                this._heldItem);
        }
    }

    [ServerCallback]
    private void Update()
    {
        if (this._heldItem != ItemType.None && !this.CanHoldItem)
        {
            this._heldItem = ItemType.None;
        }

        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null
            || gameState.CurrentPhase != GamePhase.Finished)
        {
            this._hasClearedFinishedState = false;
            return;
        }

        if (this._hasClearedFinishedState)
        {
            return;
        }

        this._hasClearedFinishedState = true;

        this.ClearServerFlights(true);
        this.ClearServerEffects();
        this.RpcClearItemWorld();
    }

    private void OnServerProjectileLaunched(
    ThrowableProjectile projectile,
    ThrowableItemDefinition definition)
    {
        this._nextFlightId++;

        if (this._nextFlightId == 0)
        {
            this._nextFlightId++;
        }

        ServerFlight flight = new ServerFlight
        {
            Id = this._nextFlightId,
            Item = definition.Item,
            NextSendTime = NetworkTime.time + this._flightSyncInterval
        };

        flight.PositionHandler =
            position => this.OnServerProjectileMoved(projectile, position);

        this._serverFlights.Add(projectile, flight);
        projectile.PositionChanged += flight.PositionHandler;

        this.RpcStartFlight(
            flight.Id,
            flight.Item,
            projectile.transform.position,
            this._flightSyncInterval);
    }

    private void OnServerProjectileMoved(
        ThrowableProjectile projectile,
        Vector3 position)
    {
        if (!this._serverFlights.TryGetValue(
                projectile, out ServerFlight flight))
        {
            return;
        }

        if (NetworkTime.time < flight.NextSendTime)
        {
            return;
        }

        flight.NextSendTime = NetworkTime.time + this._flightSyncInterval;

        this.RpcMoveFlight(flight.Id, position);
    }

    private void OnServerFlightResolved(
     ThrowableProjectile projectile,
     TrajectoryEndReason endReason,
     RaycastHit landingHit,
     bool canActivateEffect,
     WallPlacement? placement,
     Vector4 clipPlane)
    {
        if (!this._serverFlights.TryGetValue(
                projectile, out ServerFlight flight))
        {
            return;
        }

        projectile.PositionChanged -= flight.PositionHandler;
        this._serverFlights.Remove(projectile);

        this.RpcEndFlight(flight.Id);

        if (!canActivateEffect)
        {
            return;
        }

        bool hasWallPlacement = placement.HasValue;

        Vector3 groundCenter = hasWallPlacement
            ? placement.Value.GroundCenter
            : Vector3.zero;

        Quaternion rotation = hasWallPlacement
            ? placement.Value.Rotation
            : Quaternion.identity;

        Vector3 size = hasWallPlacement
            ? placement.Value.Size
            : Vector3.zero;

        this.RpcActivateImpact(
            flight.Item,
            landingHit.point,
            landingHit.normal,
            hasWallPlacement,
            groundCenter,
            rotation,
            size,
            NetworkTime.time,
            clipPlane);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ThrowableLauncher launcher = this._controller.Launcher;

        if (launcher == null)
        {
            Debug.LogError("아이템 런처를 연결하세요.", this);
            return;
        }

        launcher.ProjectileLaunched += this.OnServerProjectileLaunched;
        launcher.FlightResolved += this.OnServerFlightResolved;
        launcher.ImpactActivated += this.OnServerImpactActivated;
    }

    private void OnServerImpactActivated(GameObject effect)
    {
        this._serverEffects.RemoveAll(existing => existing == null);
        this._serverEffects.Add(effect);
    }

    private void ClearServerEffects()
    {
        foreach (GameObject effect in this._serverEffects)
        {
            if (effect != null)
            {
                effect.SetActive(false);
                Destroy(effect);
            }
        }

        this._serverEffects.Clear();
    }

    private void ClearServerFlights(bool notifyClients)
    {
        foreach (KeyValuePair<ThrowableProjectile, ServerFlight> pair
            in this._serverFlights)
        {
            ThrowableProjectile projectile = pair.Key;

            if (projectile != null)
            {
                projectile.PositionChanged -= pair.Value.PositionHandler;
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }

            if (notifyClients)
            {
                this.RpcEndFlight(pair.Value.Id);
            }
        }

        this._serverFlights.Clear();
    }

    public override void OnStopServer()
    {
        ThrowableLauncher launcher = this._controller.Launcher;

        if (launcher != null)
        {
            launcher.ProjectileLaunched -= this.OnServerProjectileLaunched;
            launcher.FlightResolved -= this.OnServerFlightResolved;
            launcher.ImpactActivated -= this.OnServerImpactActivated;
        }

        this.ClearServerFlights(false);
        this.ClearServerEffects();

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        this._controller.ApplyHeldItem(this._heldItem);
    }

    public override void OnStopClient()
    {
        this._isWaitingForThrow = false;
        this._controller.ClearLocalSelection();
        this.ClearRemoteFlights();
        this.ClearRemoteEffects();

        base.OnStopClient();
    }

    private void OnHeldItemChanged(ItemType oldItem, ItemType newItem)
    {
        if (this.isClient)
        {
            this._controller.ApplyHeldItem(newItem);
        }
    }

    [ClientRpc]
    private void RpcStartFlight(
    uint flightId,
    ItemType item,
    Vector3 position,
    float smoothingTime)
    {
        // 호스트 화면에는 실제 투척체가 이미 존재
        if (this.isServer || this._remoteFlights.ContainsKey(flightId))
        {
            return;
        }

        ThrowableItemDefinition definition =
            this._controller.FindDefinition(item);

        if (definition == null)
        {
            Debug.LogWarning($"원격 투척 아이템 정의가 없습니다: {item}", this);
            return;
        }

        GameObject visualPrefab = definition.GetFlightVisualPrefab();

        if (visualPrefab == null)
        {
            return;
        }

        GameObject root = new GameObject($"ThrowableFlight_{flightId}");
        root.transform.SetPositionAndRotation(position, Quaternion.identity);

        Instantiate(visualPrefab, root.transform, false);

        ThrowableRemoteFlight flight =
            root.AddComponent<ThrowableRemoteFlight>();

        flight.Initialize(position, smoothingTime);

        this._remoteFlights.Add(flightId, flight);
    }

    [ClientRpc]
    private void RpcMoveFlight(uint flightId, Vector3 position)
    {
        if (this.isServer)
        {
            return;
        }

        if (this._remoteFlights.TryGetValue(
                flightId, out ThrowableRemoteFlight flight)
            && flight != null)
        {
            flight.SetPosition(position);
        }
    }

    [ClientRpc]
    private void RpcActivateImpact(
    ItemType item,
    Vector3 position,
    Vector3 normal,
    bool hasWallPlacement,
    Vector3 groundCenter,
    Quaternion rotation,
    Vector3 size,
    double startedAt,
    Vector4 clipPlane)
    {
        if (this.isServer)
        {
            return;
        }

        ThrowableItemDefinition definition =
            this._controller.FindDefinition(item);

        if (definition == null || definition.ImpactEffect == null)
        {
            Debug.LogWarning($"원격 착지 효과 정의가 없습니다: {item}", this);
            return;
        }

        WallPlacement? placement = null;

        if (hasWallPlacement)
        {
            placement = WallPlacement.FromConfirmed(
                groundCenter, rotation, size);
        }

        float elapsedTime = (float)Math.Max(
            0d, NetworkTime.time - startedAt);

        GameObject effect = definition.ImpactEffect.Activate(
            position,
            normal,
            placement,
            elapsedTime,
            false,
            clipPlane);

        this._remoteEffects.RemoveAll(
            existingEffect => existingEffect == null);

        if (effect != null)
        {
            this._remoteEffects.Add(effect);
        }
    }

    private void ClearRemoteEffects()
    {
        foreach (GameObject effect in this._remoteEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        this._remoteEffects.Clear();
    }

    [ClientRpc]
    private void RpcClearItemWorld()
    {
        if (this.isServer)
        {
            return;
        }

        this.ClearRemoteFlights();
        this.ClearRemoteEffects();
    }

    [ClientRpc]
    private void RpcEndFlight(uint flightId)
    {
        if (this.isServer)
        {
            return;
        }

        if (this._remoteFlights.TryGetValue(
                flightId, out ThrowableRemoteFlight flight))
        {
            if (flight != null)
            {
                Destroy(flight.gameObject);
            }

            this._remoteFlights.Remove(flightId);
        }
    }

    private void ClearRemoteFlights()
    {
        foreach (ThrowableRemoteFlight flight in this._remoteFlights.Values)
        {
            if (flight != null)
            {
                Destroy(flight.gameObject);
            }
        }

        this._remoteFlights.Clear();
    }

    [TargetRpc]
    private void TargetCompleteHeldItemRequest(
        NetworkConnectionToClient target,
        ItemType requestedItem,
        ItemType confirmedItem,
        bool isAccepted)
    {
        this._controller.ApplyHeldItem(confirmedItem);
        this._controller.NotifyHeldItemRequestCompleted(
            requestedItem, isAccepted);

        if (!isAccepted)
        {
            Debug.LogWarning(
                $"아이템 장착 요청이 거절됐습니다: {requestedItem}",
                this);
        }
    }

    [TargetRpc]
    private void TargetCompleteThrow(
        NetworkConnectionToClient target,
        ItemType item,
        bool hasLaunched,
        ItemType confirmedItem)
    {
        this._isWaitingForThrow = false;

        this._controller.ApplyHeldItem(confirmedItem);
        this._controller.NotifyThrowRequestCompleted(item, hasLaunched);

        if (!hasLaunched)
        {
            Debug.LogWarning($"아이템 투척이 거절됐습니다: {item}", this);
        }
    }
}