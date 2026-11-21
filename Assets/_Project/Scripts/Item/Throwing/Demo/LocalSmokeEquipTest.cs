using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// 실제 인벤토리 연결 전 개발용 장착과 수량
public sealed class LocalSmokeEquipTest : NetworkBehaviour
{
    [Tooltip("개발 실행에서 임시 아이템 장착과 수량 사용")]
    [SerializeField] private bool _isEnabledForNetworkTest;

    [SyncVar]
    private bool _isUsingTestInventory;

    private readonly SyncDictionary<ItemType, int> _itemCounts =
        new SyncDictionary<ItemType, int>();

    private static readonly ItemType[] TestItems =
    {
        ItemType.VineWall,
        ItemType.FireWall,
        ItemType.WaterWall,
        ItemType.Smoke,
        ItemType.Bomb
    };

    private PlayerRoleSync _playerRole;
    private ThrowableItemUseController _useController;

    private static bool IsTestBuild
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        this._playerRole = GetComponent<PlayerRoleSync>();
        this._useController = GetComponent<ThrowableItemUseController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!IsTestBuild || !this._isEnabledForNetworkTest
            || this._useController == null
            || this._useController.HasInventoryHandlers)
        {
            return;
        }

        this._itemCounts.Clear();

        foreach (ItemType item in TestItems)
        {
            this._itemCounts.Add(item, 5);
        }

        // TODO(구형님): 실제 인벤토리의 호스트 처리로 교체
        this._useController.SetInventoryHandlers(
            this.CanUseTestItem,
            this.CompleteTestUse);

        this._isUsingTestInventory = true;
    }

    private bool CanUseTestItem(ItemType item)
    {
        return this._isEnabledForNetworkTest
            && this._isUsingTestInventory
            && this.GetTestCount(item) > 0;
    }

    private void CompleteTestUse(ItemType item, bool hasLaunched)
    {
        if (hasLaunched)
        {
            int count = this.GetTestCount(item);

            if (count <= 0)
            {
                Debug.LogError(
                    $"임시 아이템 수량과 발사 결과가 일치하지 않습니다: {item}",
                    this);
                return;
            }

            this._itemCounts[item] = count - 1;
        }

        Debug.Log(
            $"[임시 인벤토리] Player={this.netId}, Item={item}, "
            + $"발사 성공={hasLaunched}, 남은 수량={this.GetTestCount(item)}",
            this);
    }

    private int GetTestCount(ItemType item)
    {
        return this._itemCounts.TryGetValue(item, out int count)
            ? count
            : 0;
    }

    private void Update()
    {
        if (!IsTestBuild || !this._isEnabledForNetworkTest
            || !this.isLocalPlayer)
        {
            return;
        }

        GameStateManager gameState = GameStateManager.Instance;

        if (this._playerRole == null || !this._playerRole.IsRunner
            || this._playerRole.IsJailed
            || gameState == null
            || gameState.CurrentPhase != GamePhase.Playing
            || this._useController == null)
        {
            return;
        }

        ItemType item = this.GetPressedItem();

        if (item == ItemType.None)
        {
            return;
        }

        if (this._isUsingTestInventory && this.GetTestCount(item) <= 0)
        {
            Debug.Log($"임시 아이템 수량이 없습니다: {item}", this);
            return;
        }

        // TODO(구형님): 실제 인벤토리 선택 결과로 교체
        this._useController.SetHeldItem(item);
    }

    private ItemType GetPressedItem()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return ItemType.None;
        }

        if (keyboard.f5Key.wasPressedThisFrame) return ItemType.VineWall;
        if (keyboard.f6Key.wasPressedThisFrame) return ItemType.FireWall;
        if (keyboard.f7Key.wasPressedThisFrame) return ItemType.WaterWall;
        if (keyboard.f8Key.wasPressedThisFrame) return ItemType.Smoke;
        if (keyboard.f9Key.wasPressedThisFrame) return ItemType.Bomb;

        return ItemType.None;
    }

    // 개발용 수량 표시. 실제 인벤토리 UI와 별개
    private void OnGUI()
    {
        if (!IsTestBuild || !this._isEnabledForNetworkTest
            || !this.isLocalPlayer || !this._isUsingTestInventory
            || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
        {
            return;
        }

        string text =
            "[개발용 아이템 수량 / 도망자 사용]\n"
            + $"F5 덩굴 벽: {this.GetTestCount(ItemType.VineWall)}\n"
            + $"F6 불벽: {this.GetTestCount(ItemType.FireWall)}\n"
            + $"F7 수벽: {this.GetTestCount(ItemType.WaterWall)}\n"
            + $"F8 연막: {this.GetTestCount(ItemType.Smoke)}\n"
            + $"F9 폭탄: {this.GetTestCount(ItemType.Bomb)}";

        GUI.Label(new Rect(16f, 70f, 380f, 140f), text);
    }
}