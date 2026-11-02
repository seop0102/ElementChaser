
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
        animator = GetComponentInChildren<Animator>();
    }

    public void SetMoving(bool moving)
    {
        if (!isLocalPlayer)
            return;

        if (lastSentMoving == moving)
            return;

        lastSentMoving = moving;
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

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyAnimation(isMoving);
    }
}
