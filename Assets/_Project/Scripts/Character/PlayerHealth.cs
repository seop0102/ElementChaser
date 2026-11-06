
using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 4;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
            return;

        int oldHealth = currentHealth;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"[피격] {gameObject.name} 체력 {oldHealth} → {currentHealth} (-{damage})"
        );

        if (currentHealth == 0)
        {
            Debug.Log($"[체력 0] {gameObject.name} 쓰러짐!");
        }

        RpcPlayHitAnimation();

        // 피격된 플레이어의 화면만 흔들기
        TargetShakeCamera(connectionToClient);
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"체력 동기화: {oldHealth} → {newHealth}");
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
