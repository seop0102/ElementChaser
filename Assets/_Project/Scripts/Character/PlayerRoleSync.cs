
using Mirror;
using UnityEngine;

public class PlayerRoleSync : NetworkBehaviour
{
    [Header("역할별 외형")]
    [SerializeField] private Transform angelHalo;
    [SerializeField] private Transform devilHorns;

    [Header("캐릭터 모델")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private Renderer bodyRenderer;

    [Header("역할별 머티리얼")]
    [SerializeField] private Material angelMaterial;
    [SerializeField] private Material devilMaterial;

    [Header("역할별 크기")]
    [SerializeField] private float chaserScale = 1.5f;

    private Vector3 originalModelScale;

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
        if (characterModel != null)
        {
            originalModelScale = characterModel.localScale;
        }

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
        Debug.Log(
            $"[감옥 상태 변경] {gameObject.name}: {newValue}"
        );
    }

    [Server]
    public void SetRole(PlayerRole newRole)
    {
        role = newRole;
        UpdateAppearance();
    }

    private void OnRoleChanged(
        PlayerRole oldRole,
        PlayerRole newRole)
    {
        Debug.Log(
            $"[역할 변경] {gameObject.name}: {oldRole} → {newRole}"
        );

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        // 1. 천사 링 / 악마 뿔
        if (angelHalo != null)
            angelHalo.gameObject.SetActive(IsRunner);

        if (devilHorns != null)
            devilHorns.gameObject.SetActive(IsChaser);

        // 2. 술래 모델 크기
        if (characterModel != null)
        {
            characterModel.localScale = IsChaser
                ? originalModelScale * chaserScale
                : originalModelScale;
        }

        // 3. 역할별 머티리얼
        if (bodyRenderer != null)
        {
            Material targetMaterial =
                IsChaser ? devilMaterial : angelMaterial;

            if (targetMaterial != null)
                bodyRenderer.sharedMaterial = targetMaterial;
        }
    }
}

