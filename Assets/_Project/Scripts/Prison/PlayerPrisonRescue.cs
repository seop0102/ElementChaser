using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// 입력은 로컬 플레이어가 보내고, 구조 판정은 서버가 담당한다.
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerRoleSync))]
[RequireComponent(typeof(PlayerPrison))]
public class PlayerPrisonRescue : NetworkBehaviour
{
    [Header("구조 설정")]
    [SerializeField] private Key rescueKey = Key.R;
    [SerializeField, Min(0.1f)] private float rescueRange = 1.5f;
    [SerializeField, Min(0.1f)] private float rescueDuration = 5f;

    private PlayerHealth health;
    private PlayerRoleSync role;
    private PlayerPrison playerPrison;

    // 로컬 입력 상태.
    private uint localTargetId;
    private double nextSendTime;

    // 서버가 관리하는 구조 상태.
    private NetworkPrison serverTarget;
    private double rescueStartTime;
    private double lastHoldTime;
    private uint startDamageVersion;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        role = GetComponent<PlayerRoleSync>();
        playerPrison = GetComponent<PlayerPrison>();
    }

    private void Update()
    {
        if (isLocalPlayer)
            UpdateLocalInput();

        if (isServer)
            UpdateServerRescue();
    }

    private bool CanRescue()
    {
        GameStateManager game = GameStateManager.Instance;

        return game != null &&
            game.CurrentPhase == GamePhase.Playing &&
            role.IsRunner &&
            !role.IsJailed &&
            !playerPrison.IsJailed &&
            !health.IsDead &&
            health.CurrentHealth > 0;
    }

    private void UpdateLocalInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (!Application.isFocused ||
            keyboard == null ||
            !CanRescue())
        {
            StopLocalRescue();
            return;
        }

        var key = keyboard[rescueKey];

        if (!key.isPressed)
        {
            StopLocalRescue();
            return;
        }

        // 누르기 시작한 순간 가장 가까운 감옥을 선택한다.
        if (key.wasPressedThisFrame)
        {
            NetworkPrison nearest = FindNearestPrison();

            localTargetId = nearest != null
                ? nearest.netId
                : 0;

            nextSendTime = 0d;
        }

        if (localTargetId == 0)
            return;

        // 키를 계속 누르고 있다는 신호를 초당 10번 전송한다.
        if (NetworkTime.time >= nextSendTime)
        {
            nextSendTime = NetworkTime.time + 0.1d;
            CmdHoldRescue(localTargetId);
        }
    }

    private NetworkPrison FindNearestPrison()
    {
        NetworkPrison nearest = null;
        float nearestDistance = rescueRange;

        foreach (NetworkIdentity identity in
                 NetworkClient.spawned.Values)
        {
            if (identity == null)
                continue;

            NetworkPrison prison =
                identity.GetComponent<NetworkPrison>();

            if (prison == null ||
                prison.IsDropping ||
                prison.RemainingTime <= 0d)
            {
                continue;
            }

            float distance =
                prison.GetInteractionDistance(transform.position);

            if (distance <= nearestDistance)
            {
                nearest = prison;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void StopLocalRescue()
    {
        if (localTargetId == 0)
            return;

        localTargetId = 0;
        CmdStopRescue();
    }

    [Command]
    private void CmdHoldRescue(uint prisonId)
    {
        // 클라이언트가 보낸 대상과 거리를 서버에서도 검사한다.
        if (!NetworkServer.spawned.TryGetValue(
                prisonId, out NetworkIdentity identity))
        {
            ResetServerRescue();
            return;
        }

        NetworkPrison target =
            identity.GetComponent<NetworkPrison>();

        if (!IsValidTarget(target))
        {
            ResetServerRescue();
            return;
        }

        if (serverTarget != target)
        {
            serverTarget = target;
            rescueStartTime = NetworkTime.time;
            startDamageVersion = health.DamageVersion;
        }

        lastHoldTime = NetworkTime.time;
    }

    [Command]
    private void CmdStopRescue()
    {
        ResetServerRescue();
    }

    private bool IsValidTarget(NetworkPrison target)
    {
        return CanRescue() &&
            target != null &&
            !target.IsDropping &&
            target.RemainingTime > 0d &&
            target.GetInteractionDistance(transform.position)
                <= rescueRange;
    }

    [Server]
    private void UpdateServerRescue()
    {
        if (serverTarget == null)
            return;

        // 거리 이탈, 피격, 입력 신호 중단 시 진행도를 초기화한다.
        if (!IsValidTarget(serverTarget) ||
            health.DamageVersion != startDamageVersion ||
            NetworkTime.time - lastHoldTime > 0.5d)
        {
            ResetServerRescue();
            return;
        }

        if (NetworkTime.time - rescueStartTime < rescueDuration)
            return;

        NetworkPrison rescuedPrison = serverTarget;
        ResetServerRescue();

        rescuedPrison.ServerRescueAll();
    }

    private void ResetServerRescue()
    {
        serverTarget = null;
        rescueStartTime = 0d;
        lastHoldTime = 0d;
        startDamageVersion = 0;
    }
}