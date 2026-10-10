
using Mirror;
using UnityEngine;

public class PlayerRoleSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnRoleChanged))]
    private PlayerRole role = PlayerRole.Runner;

    public PlayerRole Role => role;

    public bool IsChaser => role == PlayerRole.Chaser;
    public bool IsRunner => role == PlayerRole.Runner;

    [Server]
    public void SetRole(PlayerRole newRole)
    {
        role = newRole;
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Debug.Log($"[역할 변경] {gameObject.name}: {oldRole} → {newRole}");
    }
}
