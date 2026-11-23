using System;
using UnityEngine;

// 투척 아이템 선택, 조준 표시, 사용
public sealed class ThrowableItemUseController : MonoBehaviour
{
    [SerializeField] private ThrowableLauncher _launcher;
    [SerializeField] private ThrowablePreviewRenderer _previewRenderer;
    [SerializeField] private Transform _heldVisualAnchor;

    [Tooltip("장착할 수 있는 아이템 정의")]
    [SerializeField] private ThrowableItemDefinition[] _itemDefinitions;

    //인벤 아이템 종류 전달-> 필요한 정보 어디서 가져올지
    [Tooltip("투척 경로 검사에 사용할 레이어")]  
    [SerializeField] private LayerMask _defaultCollisionMask;   

    [Tooltip("원형 착지 미리보기 반지름")]
    [SerializeField, Min(0f)] private float _defaultLandingRadius = 3f;

    private ThrowableItemDefinition _selectedItem;
    private GameObject _heldVisual;
    private LayerMask _collisionMask;
    private Func<RaycastHit, bool> _isLandingSurface;
    private Func<RaycastHit, bool> _canUseAtLanding;
    private float _landingRadius;
    private Func<ItemType, bool> _canUseItem;
    private Action<ItemType, bool> _onUseCompleted;
    private bool _isProcessingUse;
    private ThrowableItemNetwork _network;
    private PlayerInventory _inventory;
    public bool HasSelection => this._selectedItem != null;
    public ItemType HeldItem => this._selectedItem != null ? this._selectedItem.Item : ItemType.None;

    // 실제 장착 상태 변경 알림
    public event Action<ItemType> HeldItemChanged; 

    // 요청한 아이템과 호스트의 장착 승인 여부
    public event Action<ItemType, bool> HeldItemRequestCompleted;

    // 요청자에게 전달하는 실제 발사 결과
    public event Action<ItemType, bool> ThrowRequestCompleted;

    internal LayerMask ThrowCollisionMask => this._defaultCollisionMask;
    internal ThrowableLauncher Launcher => this._launcher;
    public bool HasInventoryHandlers =>this._canUseItem != null && this._onUseCompleted != null;

    private void Awake()
    {
        this._network = GetComponent<ThrowableItemNetwork>();
        this._inventory = GetComponent<PlayerInventory>();

        if (this._inventory == null)
        {
            Debug.LogError("PlayerInventory를 찾을 수 없습니다.", this);
            return;
        }

        // 인벤토리 보유 확인 및 사용 결과 처리 연결
        this.SetInventoryHandlers(
            item => this._inventory.GetItemCount(item) > 0,
            (item, hasLaunched) =>
            {
                if (hasLaunched)
                {
                    this._inventory.TryUseItem(item);
                }
            });
    }

    // 인벤토리의 보유 확인과 사용 결과 처리 연결
    public void SetInventoryHandlers(
        Func<ItemType, bool> canUseItem,
        Action<ItemType, bool> onUseCompleted)
    {
        if (canUseItem == null)
        {
            throw new ArgumentNullException(nameof(canUseItem));
        }

        if (onUseCompleted == null)
        {
            throw new ArgumentNullException(nameof(onUseCompleted));
        }

        this._canUseItem = canUseItem;
        this._onUseCompleted = onUseCompleted;
    }

    // 인벤토리에서 요청한 장착 상태 전달
    public void SetHeldItem(ItemType item)
    {
        if (this._network != null && this._network.IsNetworkActive)
        {
            this._network.RequestHeldItem(item);
            return;
        }

        this.ApplyHeldItem(item);
    }

    // 인벤토리에서 선택한 아이템 장착
    internal void ApplyHeldItem(ItemType item)
    {
        if (item == ItemType.None)
        {
            this.ClearLocalSelection();
            return;
        }

        if (this.HeldItem == item)
        {
            return;
        }

        //연결 오류
        ThrowableItemDefinition definition = this.FindDefinition(item);

        if (definition == null)
        {
            Debug.LogWarning($"장착할 아이템 정의를 확인하세요: {item}", this);
            return;
        }

        if (definition.ImpactEffect == null)
        {
            Debug.LogWarning($"아이템의 착지 효과를 연결하세요: {item}", this);
            return;
        }

        if (this._launcher == null || this._previewRenderer == null)
        {
            Debug.LogWarning("투척 런처와 미리보기 표시를 연결하세요.", this);
            return;
        }

        if (definition.HeldVisualPrefab != null
            && this._heldVisualAnchor == null)
        {
            Debug.LogWarning("아이템을 표시할 손 위치를 연결하세요.", this);
            return;
        }

        if (this._defaultCollisionMask.value == 0)
        {
            Debug.LogWarning("투척 충돌 레이어를 설정하세요.", this);
            return;
        }

        this.SelectItem(
            definition,
            this._defaultLandingRadius,
            this._defaultCollisionMask,
            IsTileTop,
            IsTileTop);
    }
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

        ItemType previousItem = this.HeldItem;

        this.ClearSelection();

        this._selectedItem = itemDefinition;
        this._landingRadius = landingRadius;
        this._collisionMask = collisionMask;
        this._isLandingSurface = isLandingSurface;
        this._canUseAtLanding = canUseAtLanding;

        if (itemDefinition.HeldVisualPrefab != null)
        {
            this._heldVisual = Instantiate(
                itemDefinition.HeldVisualPrefab,
                this._heldVisualAnchor.position,
                this._heldVisualAnchor.rotation);

            // 손 위치를 따라가되 외형의 월드 크기는 유지
            this._heldVisual.transform.SetParent(
                this._heldVisualAnchor,
                true);
        }

        if (previousItem != this.HeldItem)
        {
            this.HeldItemChanged?.Invoke(this.HeldItem);
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

        WallPlacementSettings wallSettings = this._selectedItem.WallSettings;

        if (wallSettings == null)
        {
            this._previewRenderer.Show(
                prediction, canLaunch, this._landingRadius);
            return;
        }

        if (prediction.TryGetLandingHit(out RaycastHit landingHit)
            && WallPlacement.TryCreate(
                landingHit.point,
                direction,
                wallSettings,
                out WallPlacement placement))
        {
            bool canPlaceWall =
                canLaunch && this._launcher.IsWallPlacementClear(placement);

            this._previewRenderer.Show(prediction, canPlaceWall, 0f, placement);
        }
        else
        {
            this._previewRenderer.Show(prediction, false, 0f);
        }
    }

    public bool TryThrow(Vector3 origin, Vector3 direction)
    {
        if (this._network != null && this._network.IsNetworkActive)
        {
            Debug.LogWarning("네트워크 투척은 호스트 요청 경로로 실행해야 합니다. 직접 호출은 허용되지 않습니다.",this);
            return false;
        }

        if (!this.HasSelection || this._isProcessingUse)
        {
            return false;
        }

        if (!this.HasInventoryHandlers)
        {
            Debug.LogWarning("인벤토리 보유 확인과 사용 결과 처리를 연결하세요.", this);
            return false;
        }

        ItemType item = this.HeldItem;
        ThrowableItemDefinition definition = this._selectedItem;

        Func<ItemType, bool> canUseItem = this._canUseItem;
        Action<ItemType, bool> onUseCompleted = this._onUseCompleted;

        this._isProcessingUse = true;

        try
        {
            // TODO(네트워크): 보유 확인과 결과 처리는 호스트에서 실행
            if (!canUseItem(item))
            {
                onUseCompleted(item, false);
                return false;
            }

            bool hasLaunched = this._launcher.TryLaunch(
                definition,
                origin,
                direction,
                this._collisionMask,
                this._isLandingSurface,
                this._canUseAtLanding);

            if (hasLaunched)
            {
                this.CancelSelection();
            }

            onUseCompleted(item, hasLaunched);

            return hasLaunched;
        }
        finally
        {
            this._isProcessingUse = false;
        }
    }
    public void CancelSelection()
    {
        this.SetHeldItem(ItemType.None);
    }

    internal void ClearLocalSelection()
    {
        ItemType previousItem = this.HeldItem;

        this.ClearSelection();

        if (previousItem != ItemType.None)
        {
            this.HeldItemChanged?.Invoke(ItemType.None);
        }
    }

    // 장착 데이터와 표시 정리
    private void ClearSelection()
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

    public void RequestThrow(Vector3 origin, Vector3 direction)
    {
        if (this._network != null && this._network.IsNetworkActive)
        {
            this._network.RequestThrow(origin, direction);
            return;
        }

        this.TryThrow(origin, direction);
    }

    // 호스트가 사용할 인벤토리 연결 조회
    internal bool TryGetInventoryHandlers(
        out Func<ItemType, bool> canUseItem,
        out Action<ItemType, bool> onUseCompleted)
    {
        canUseItem = this._canUseItem;
        onUseCompleted = this._onUseCompleted;

        return canUseItem != null && onUseCompleted != null;
    }

    // 호스트가 확정한 아이템 투척
    internal bool TryLaunchServerItem(
        ItemType item,
        Vector3 origin,
        Vector3 direction)
    {
        if (this._network == null || !this._network.isServer)
        {
            return false;
        }

        ThrowableItemDefinition definition = this.FindDefinition(item);

        if (definition == null || definition.ImpactEffect == null
            || this._launcher == null
            || this._defaultCollisionMask.value == 0)
        {
            Debug.LogWarning("호스트의 아이템 정의와 투척 설정을 확인하세요.", this);
            return false;
        }

        return this._launcher.TryLaunch(
            definition,
            origin,
            direction,
            this._defaultCollisionMask,
            IsTileTop,
            IsTileTop);
    }

    internal void NotifyThrowRequestCompleted(
        ItemType item,
        bool hasLaunched)
    {
        this.ThrowRequestCompleted?.Invoke(item, hasLaunched);
    }

    // 종류에 해당하는 아이템 정의 검색
    internal ThrowableItemDefinition FindDefinition(ItemType item)
    {
        if (this._itemDefinitions == null)
        {
            return null;
        }

        ThrowableItemDefinition result = null;

        foreach (ThrowableItemDefinition definition in this._itemDefinitions)
        {
            if (definition == null || definition.Item != item)
            {
                continue;
            }

            if (result != null)
            {
                Debug.LogError($"같은 종류의 아이템 정의가 중복됐습니다: {item}", this);
                return null;
            }

            result = definition;
        }

        return result;
    }

    internal void NotifyHeldItemRequestCompleted(ItemType item, bool isAccepted)
    {
        this.HeldItemRequestCompleted?.Invoke(item, isAccepted);
    }

    // 현재 맵의 타일 윗면 판정
    private static bool IsTileTop(RaycastHit hit)
    {
        return hit.collider != null
            && hit.collider.GetComponentInParent<Tile>() != null
            && Vector3.Dot(hit.normal, Vector3.up) >= 0.9f;
    }

    private void OnDisable()
    {
        this.ClearLocalSelection();
    }
}