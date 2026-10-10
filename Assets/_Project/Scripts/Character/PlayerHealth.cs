
using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    public int CurrentHealth => currentHealth;

    public override void OnStartServer()
    {
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
    }


    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log(
            $"체력 동기화: {oldHealth} → {newHealth}"
        );
    }
}
