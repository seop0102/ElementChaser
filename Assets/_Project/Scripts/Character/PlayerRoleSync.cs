
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

    private void Awake()
    {
        // 네트워크 연결 전에도 기본 외형 적용
        UpdateAppearance();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateAppearance();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
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
        UpdateAppearance();
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
        else
            Debug.LogWarning($"[RoleSync] {name}: Angel Halo 미연결");

        if (devilHorns != null)
            devilHorns.gameObject.SetActive(IsChaser);
        else
            Debug.LogWarning($"[RoleSync] {name}: Devil Horns 미연결");
    }
}

