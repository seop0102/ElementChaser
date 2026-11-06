
using Mirror;
using UnityEngine;

public class PlayerAnimationSync : NetworkBehaviour
{
    private Animator animator;
    private bool lastSentMoving;

    [SyncVar(hook = nameof(OnMovingChanged))]
    private bool isMoving;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyAnimation(isMoving);
    }

    public void SetMoving(bool moving)
    {
        if (!isLocalPlayer)
            return;

        if (lastSentMoving == moving)
            return;

        lastSentMoving = moving;

        // 로컬에서는 즉시 이동 애니메이션 적용
        ApplyAnimation(moving);

        // 다른 클라이언트에도 이동 상태 동기화
        CmdSetMoving(moving);
    }

    [Command]
    private void CmdSetMoving(bool moving)
    {
        isMoving = moving;
    }

    private void OnMovingChanged(bool oldValue, bool newValue)
    {
        ApplyAnimation(newValue);
    }

    private void ApplyAnimation(bool moving)
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", moving ? 1f : 0f);
    }

    // PlayerAttack, Hit
    public void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Attack");
    }
    public void PlayHitAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Hit");
    }

}
