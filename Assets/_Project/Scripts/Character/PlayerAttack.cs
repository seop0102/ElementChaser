
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerAttack : NetworkBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField, Range(0f, 360f)]
    private float attackAngle = 100f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 1;

    [Header("공격 판정")]
    [SerializeField] private LayerMask obstacleMask;

    private float nextAttackTime;
    private PlayerRoleSync playerRole;
    private PlayerAnimationSync animationSync;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerRole = GetComponent<PlayerRoleSync>();
        animationSync = GetComponent<PlayerAnimationSync>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;
        if (playerHealth == null || playerHealth.IsDead)
        return;

        if (playerRole == null || !playerRole.IsChaser)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
            return;

        CmdAttack();
    }

    [Command]
    private void CmdAttack()
    {
        if (playerHealth == null || playerHealth.IsDead)
            return;
        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
            return;

        if (playerRole == null || !playerRole.IsChaser)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        // 공격 애니메이션은 기존 방식 유지
        RpcPlayAttackAnimation();

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        int hitCount = 0;

        // 서버에 존재하는 모든 플레이어 검사
        foreach (NetworkIdentity identity
                 in NetworkServer.spawned.Values)
        {
            if (identity == null)
                continue;

            PlayerHealth target =
                identity.GetComponent<PlayerHealth>();

            if (target == null ||
                target == GetComponent<PlayerHealth>())
                continue;

            PlayerRoleSync targetRole =
                identity.GetComponent<PlayerRoleSync>();

            // 도망자만 공격
            if (targetRole == null || !targetRole.IsRunner)
                continue;

            // 이미 체력이 0이면 제외
            if (target.CurrentHealth <= 0)
                continue;

            Vector3 targetPosition = target.transform.position;

            // 높이 차이를 제외한 수평 거리 계산
            Vector3 direction = targetPosition - origin;
            direction.y = 0f;

            float distance = direction.magnitude;

            // 사거리 밖이면 제외
            if (distance > attackRange)
                continue;

            // 전방 부채꼴 각도 검사
            float angle = Vector3.Angle(forward, direction);

            if (angle > attackAngle * 0.5f)
                continue;

            // 벽이 공격을 가로막는지 검사
            Vector3 rayOrigin = origin + Vector3.up;
            Vector3 rayTarget = targetPosition + Vector3.up;
            Vector3 rayDirection = rayTarget - rayOrigin;

            if (Physics.Raycast(
                rayOrigin,
                rayDirection.normalized,
                rayDirection.magnitude,
                obstacleMask))
            {
                continue;
            }

            // 공격 성공
            target.TakeDamage(attackDamage);
            hitCount++;

            Debug.Log(
                $"[공격 성공] {name} → {target.name}, " +
                $"거리 {distance:F2}, 각도 {angle:F1}"
            );
        }

        if (hitCount == 0)
        {
            Debug.Log($"[공격 실패] {name}: 범위 내 대상 없음");
        }
    }

    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        if (animationSync != null)
        {
            animationSync.PlayAttackAnimation();
        }
    }

    // Scene 뷰에서 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        int segments = 24;

        Vector3 previousPoint =
            origin +
            Quaternion.Euler(0f, -attackAngle / 2f, 0f)
            * forward * attackRange;

        Gizmos.DrawLine(origin, previousPoint);

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                -attackAngle / 2f +
                attackAngle * i / segments;

            Vector3 point =
                origin +
                Quaternion.Euler(0f, angle, 0f)
                * forward * attackRange;

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        Gizmos.DrawLine(origin, previousPoint);
    }
}
