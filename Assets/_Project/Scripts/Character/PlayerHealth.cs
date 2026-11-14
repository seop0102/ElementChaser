
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

        int oldHealth = currentHealth;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"[피격] {gameObject.name} 체력 " +
            $"{oldHealth} → {currentHealth} (-{damage})"
        );

        RpcPlayHitAnimation();

        if (currentHealth == 0)
        {
            isDead = true;

            Debug.Log(
                $"[사망] {gameObject.name} 관전 모드 전환"
            );
        }
        else
        {
            // 살아 있을 때만 카메라 흔들기
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
