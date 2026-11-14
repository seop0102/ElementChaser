
using Mirror;
using UnityEngine;

public class PlayerRoleSync : NetworkBehaviour
{
    [Header("역할별 외형")]
    [SerializeField] private Transform angelHalo;
    [SerializeField] private Transform devilHorns;

    [SyncVar(hook = nameof(OnRoleChanged))]
    private PlayerRole role = PlayerRole.Runner;

    public PlayerRole Role => role;

    public bool IsChaser => role == PlayerRole.Chaser;
    public bool IsRunner => role == PlayerRole.Runner;

    [SyncVar(hook = nameof(OnJailChanged))]
    private bool isJailed = false;

    public bool IsJailed => isJailed;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateAppearance();
    }

    [Server]
    public void SetJailed(bool jailed)
    {
        isJailed = jailed;
    }

    private void OnJailChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[감옥 상태 변경] {gameObject.name}: {newValue}");
    }

    [Server]
    public void SetRole(PlayerRole newRole)
    {
        role = newRole;
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Debug.Log($"[역할 변경] {gameObject.name}: {oldRole} → {newRole}");
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (angelHalo != null)
            angelHalo.gameObject.SetActive(IsRunner);

        if (devilHorns != null)
            devilHorns.gameObject.SetActive(IsChaser);
    }
}
