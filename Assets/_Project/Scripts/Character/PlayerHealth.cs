
using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 4;
    [Header("Death Ground Alignment")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private float deathGroundOffset = 0f;

    // 무적 종료 시각은 서버에서만 판정한다.
    private double invincibleUntil;

    // 실제 피해를 받은 횟수. 구조 중 피격 여부를 확인한다.
    public uint DamageVersion { get; private set; }
    private Vector3 originalModelLocalPosition;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    [SyncVar(hook = nameof(OnDeathChanged))]
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;


[TargetRpc]
private void TargetPlayHitEffect(
    NetworkConnectionToClient target)
{
    HitEffectUI hitEffect =
        FindFirstObjectByType<HitEffectUI>();

    if (hitEffect != null)
    {
        hitEffect.Flash();
    }
    else
    {
        Debug.LogWarning("[HitEffect] UI를 찾지 못했습니다.");
    }
}

    private void Awake()
{
    if (characterModel == null)
    {
        Animator modelAnimator =
            GetComponentInChildren<Animator>(true);

        if (modelAnimator != null)
        {
            characterModel = modelAnimator.transform;
        }
    }

    if (characterModel != null)
    {
        originalModelLocalPosition =
            characterModel.localPosition;
    }
}

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentHealth = maxHealth;
        isDead = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 최초 접속 시에도 사망 상태 적용
        ApplyDeathState();
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || currentHealth <= 0)
            return;

        // 구조 직후 무적 중에는 피해를 받지 않는다.
        if (NetworkTime.time < invincibleUntil)
            return;

        PlayerRoleSync role = GetComponent<PlayerRoleSync>();
        PlayerPrison prison = GetComponent<PlayerPrison>();

        // 이번 체력 감소 → 감금 흐름은 도망자에게만 적용한다.
        if (role == null || !role.IsRunner)
            return;

        // 연결이 빠진 상태에서 체력만 0이 되는 것을 방지한다.
        if (prison == null || !prison.IsConfigured)
        {
            Debug.LogError(
                "PlayerPrison 컴포넌트와 Cage Prefab 연결을 확인해 주세요.",
                this
            );
            return;
        }

        // 감금 중에는 추가 피해를 받지 않는다.
        if (prison.IsJailed)
            return;

        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // 감금·무적 검사까지 통과한 실제 피격만 기록한다.
        DamageVersion++;

        Debug.Log(
            $"[피격] {name} 체력 {oldHealth} → {currentHealth}"
        );

        
RpcPlayHitAnimation();
RpcPlayHitFlash(); // 추가!

if (connectionToClient != null)
    TargetPlayHitEffect(connectionToClient);


        if (currentHealth == 0)
        {
            // 즉시 사망시키지 않고 감금 상태로 전환한다.
            prison.ServerEnterPrison();
        }
        else
        {
            // 살아있을 때만 카메라 흔들림을 적용한다.
            TargetShakeCamera(connectionToClient);
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log(
            $"체력 동기화: {oldHealth} → {newHealth}"
        );
    }

    private void OnDeathChanged(bool oldValue, bool newValue)
    {
        Debug.Log(
            $"[사망 상태 변경] {gameObject.name}: {newValue}"
        );

        ApplyDeathState();
    }

    private void ApplyDeathState()
{
    if (characterModel != null)
    {
        if (isDead)
        {
            Vector3 position = originalModelLocalPosition;
            position.y += deathGroundOffset;
            characterModel.localPosition = position;
        }
        else
        {
            characterModel.localPosition = originalModelLocalPosition;
        }
    }

    if (!isLocalPlayer)
        return;

    if (isDead)
    {
        Debug.Log("[관전] 사망 상태 확인");
    }
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

[ClientRpc]
private void RpcPlayHitFlash()
{
    PlayerHitFlash hitFlash =
        GetComponent<PlayerHitFlash>();

    if (hitFlash == null)
    {
        hitFlash = GetComponentInChildren<PlayerHitFlash>(true);
    }

    if (hitFlash != null)
    {
        hitFlash.PlayHitFlash();
    }
}

    // 감금 시간이 끝났을 때 서버가 완전 탈락 처리한다.
    [Server]
    public void ServerEliminate()
    {
        if (isDead)
            return;

        currentHealth = 0;
        isDead = true;

        Debug.Log($"[Eliminate] Player {netId} 완전 탈락");
    }

    [Server]
    public void ServerRecoverFromPrison()
    {
        // 완전히 탈락한 플레이어는 구조로 되살리지 않는다.
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(2, 1, maxHealth);
        invincibleUntil = NetworkTime.time + 2d;
    }
}
