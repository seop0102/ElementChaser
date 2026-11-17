
using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 4;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    [SyncVar(hook = nameof(OnDeathChanged))]
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentHealth = maxHealth;
        isDead = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 최초 접속 시에도 사망 상태 적용
        ApplyDeathState();
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || currentHealth <= 0)
            return;

        PlayerRoleSync role = GetComponent<PlayerRoleSync>();
        PlayerPrison prison = GetComponent<PlayerPrison>();

        // 이번 체력 감소 → 감금 흐름은 도망자에게만 적용한다.
        if (role == null || !role.IsRunner)
            return;

        // 연결이 빠진 상태에서 체력만 0이 되는 것을 방지한다.
        if (prison == null || !prison.IsConfigured)
        {
            Debug.LogError(
                "PlayerPrison 컴포넌트와 Cage Prefab 연결을 확인해 주세요.",
                this
            );
            return;
        }

        // 감금 중에는 추가 피해를 받지 않는다.
        if (prison.IsJailed)
            return;

        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"[피격] {name} 체력 {oldHealth} → {currentHealth}"
        );

        RpcPlayHitAnimation();

        if (currentHealth == 0)
        {
            // 즉시 사망시키지 않고 감금 상태로 전환한다.
            prison.ServerEnterPrison();
        }
        else
        {
            // 살아있을 때만 카메라 흔들림을 적용한다.
            TargetShakeCamera(connectionToClient);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log(
            $"체력 동기화: {oldHealth} → {newHealth}"
        );
    }

    private void OnDeathChanged(bool oldValue, bool newValue)
    {
        Debug.Log(
            $"[사망 상태 변경] {gameObject.name}: {newValue}"
        );

        ApplyDeathState();
    }

    private void ApplyDeathState()
    {
        if (!isLocalPlayer)
            return;

        if (isDead)
        {
            Debug.Log("[관전] 사망 상태 확인");
            // 다음 단계에서 PlayerSpectator와 연결
        }
    }

    [ClientRpc]
    private void RpcPlayHitAnimation()
    {
        PlayerAnimationSync animationSync =
            GetComponent<PlayerAnimationSync>();

        if (animationSync != null)
        {
            animationSync.PlayHitAnimation();
        }
    }

    [TargetRpc]
    private void TargetShakeCamera(
        NetworkConnectionToClient target)
    {
        PlayerCameraShake cameraShake =
            GetComponentInChildren<PlayerCameraShake>(true);

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }
    }
}
