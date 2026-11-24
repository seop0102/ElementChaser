using System;
using Mirror;
using UnityEngine;

// 호스트가 투명화 지속시간과 해제를 관리
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerRoleSync))]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerInvisibility : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnEndTimeChanged))]
    private double _endsAt;

    private PlayerRoleSync _playerRole;
    private PlayerHealth _playerHealth;
    private bool _isReportedInvisible;

    public bool IsInvisible =>
        this.isActiveAndEnabled
        && (this.isServer || this.isClient)
        && this.CanStayInvisible
        && this._endsAt > NetworkTime.time;

    public float RemainingTime =>
        this.IsInvisible
            ? (float)Math.Max(0d, this._endsAt - NetworkTime.time)
            : 0f;

    public event Action<bool> InvisibilityChanged;

    // 투명화 상태를 유지할 수 있는지 확인 /체력(->감금), 역할, 게임 상태 등
    private bool CanStayInvisible =>
        this._playerRole != null
        && this._playerRole.IsRunner
        && !this._playerRole.IsJailed
        && this._playerHealth != null
        && !this._playerHealth.IsDead
        && this._playerHealth.CurrentHealth > 0
        && GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentPhase == GamePhase.Playing;

    private void Awake()
    {
        this._playerRole = this.GetComponent<PlayerRoleSync>();
        this._playerHealth = this.GetComponent<PlayerHealth>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        this.NotifyStateChanged();
    }

    // 사용 성공 여부만 반환하며 수량은 직접 차감하지 않음
    [Server]
    public bool TryActivate(InvisibilityEffectSettings settings)
    {
        if (settings == null
            || !(settings.Duration > 0f)
            || float.IsInfinity(settings.Duration))
        {
            Debug.LogError("투명화 설정을 확인하세요.", this);
            return false;
        }

        if (!this.CanStayInvisible || this.IsInvisible)
        {
            return false;
        }

        this._endsAt = NetworkTime.time + settings.Duration;
        this.NotifyStateChanged();

        return true;
    }

    [Server]
    public void ClearInvisibility()
    {
        if (this._endsAt <= 0d)
        {
            return;
        }

        this._endsAt = 0d;
        this.NotifyStateChanged();
    }

    private void Update()
    {
        if (this.isServer
            && this._endsAt > 0d
            && (!this.CanStayInvisible
                || NetworkTime.time >= this._endsAt))
        {
            this.ClearInvisibility();
        }

        this.NotifyStateChanged();
    }

    private void OnEndTimeChanged(
        double oldEndTime,
        double newEndTime)
    {
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        bool isInvisible = this.IsInvisible;

        if (this._isReportedInvisible == isInvisible)
        {
            return;
        }

        this._isReportedInvisible = isInvisible;

        try
        {
            this.InvisibilityChanged?.Invoke(isInvisible);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    public override void OnStopServer()
    {
        this._endsAt = 0d;
        this.NotifyStateChanged();

        base.OnStopServer();
    }

    private void OnDisable()
    {
        if (this.isServer)
        {
            this._endsAt = 0d;
        }

        this.NotifyStateChanged();
    }
}