
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerAttack : NetworkBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 1;

    private Camera playerCamera;
    private float nextAttackTime;

    private PlayerRoleSync playerRole;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>(true);
        playerRole = GetComponent<PlayerRoleSync>();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        CmdAttack();
    }

    [Command]
    private void CmdAttack()
    {
        if (playerRole == null || !playerRole.IsChaser)
            return;
        // 쿨타임은 서버에서 검사
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        // 플레이어의 몸을 기준으로 전방 공격
        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            attackRange))
        {
            PlayerHealth target =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null && target != GetComponent<PlayerHealth>())
            {
                target.TakeDamage(attackDamage);

                Debug.Log(
                    $"[공격 성공] {name} → {target.name}, 데미지 {attackDamage}"
                );

                return;
            }
        }

        Debug.Log($"[공격 실패] {name}: 사거리 내 대상 없음");
    }
}
