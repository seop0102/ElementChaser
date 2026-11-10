using System;
using UnityEngine;

// 투척 아이템 선택, 조준 표시, 사용
public sealed class ThrowableItemUseController : MonoBehaviour
{
    [SerializeField] private ThrowableLauncher _launcher;
    [SerializeField] private ThrowablePreviewRenderer _previewRenderer;
    [SerializeField] private Transform _heldVisualAnchor;

    private ThrowableItemDefinition _selectedItem;
    private GameObject _heldVisual;
    private LayerMask _collisionMask;
    private Func<RaycastHit, bool> _isLandingSurface;
    private Func<RaycastHit, bool> _canUseAtLanding;
    private float _landingRadius;

    public bool HasSelection => this._selectedItem != null;

    public void SelectItem(
        ThrowableItemDefinition itemDefinition,
        float landingRadius,
        LayerMask collisionMask,
        Func<RaycastHit, bool> isLandingSurface,
        Func<RaycastHit, bool> canUseAtLanding)
    {
        if (itemDefinition == null)
        {
            throw new ArgumentNullException(nameof(itemDefinition));
        }

        if (isLandingSurface == null)
        {
            throw new ArgumentNullException(nameof(isLandingSurface));
        }

        if (canUseAtLanding == null)
        {
            throw new ArgumentNullException(nameof(canUseAtLanding));
        }

        this.CancelSelection();

        this._selectedItem = itemDefinition;
        this._landingRadius = landingRadius;
        this._collisionMask = collisionMask;
        this._isLandingSurface = isLandingSurface;
        this._canUseAtLanding = canUseAtLanding;

        if (itemDefinition.HeldVisualPrefab != null)
        {
            this._heldVisual = Instantiate(
                itemDefinition.HeldVisualPrefab,
                this._heldVisualAnchor,
                false);
        }
    }

    public void UpdateAim(Vector3 origin, Vector3 direction)
    {
        if (!this.HasSelection)
        {
            return;
        }

        TrajectoryResult prediction = this._launcher.GetPreview(
            origin,
            direction,
            this._collisionMask,
            this._isLandingSurface,
            this._canUseAtLanding,
            out bool canLaunch);

        this._previewRenderer.Show(
            prediction, canLaunch, this._landingRadius);
    }

    public bool TryThrow(Vector3 origin, Vector3 direction)
    {
        if (!this.HasSelection)
        {
            return false;
        }

        bool hasLaunched = this._launcher.TryLaunch(
            this._selectedItem,
            origin,
            direction,
            this._collisionMask,
            this._isLandingSurface,
            this._canUseAtLanding);

        if (hasLaunched)
        {
            this.CancelSelection();
        }

        return hasLaunched;
    }

    public void CancelSelection()
    {
        this._selectedItem = null;
        this._isLandingSurface = null;
        this._canUseAtLanding = null;

        if (this._heldVisual != null)
        {
            this._heldVisual.SetActive(false);
            Destroy(this._heldVisual);
            this._heldVisual = null;
        }

        if (this._previewRenderer != null)
        {
            this._previewRenderer.Hide();
        }
    }

    private void OnDisable()
    {
        this.CancelSelection();
    }
}