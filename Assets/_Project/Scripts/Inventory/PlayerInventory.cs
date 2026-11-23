
using System;
using Mirror;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("아이템 보유 수량")]

    [SyncVar(hook = nameof(OnSmokeCountChanged))]
    [SerializeField, Min(0)] private int smokeCount = 0;

    [SyncVar(hook = nameof(OnInvisibilityCountChanged))]
    [SerializeField, Min(0)] private int invisibilityCount = 0;

    [SyncVar(hook = nameof(OnVineWallCountChanged))]
    [SerializeField, Min(0)] private int vineWallCount = 0;

    [SyncVar(hook = nameof(OnWaterWallCountChanged))]
    [SerializeField, Min(0)] private int waterWallCount = 0;

    [SyncVar(hook = nameof(OnFireWallCountChanged))]
    [SerializeField, Min(0)] private int fireWallCount = 0;

    [SyncVar(hook = nameof(OnBombCountChanged))]
    [SerializeField, Min(0)] private int bombCount = 0;


    [Header("원소 포인트")]

    [SyncVar(hook = nameof(OnWaterPointsChanged))]
    [SerializeField, Min(0)] private int waterPoints = 0;

    [SyncVar(hook = nameof(OnFirePointsChanged))]
    [SerializeField, Min(0)] private int firePoints = 0;

    [SyncVar(hook = nameof(OnGrassPointsChanged))]
    [SerializeField, Min(0)] private int grassPoints = 0;

    [SyncVar(hook = nameof(OnWindPointsChanged))]
    [SerializeField, Min(0)] private int windPoints = 0;


    // 인벤토리 변경 이벤트
    public event Action InventoryChanged;


    // ==========================================
    // 아이템 지급
    // ==========================================

    [Server]
    public void AddItem(ItemType item, int amount = 1)
    {
        if (amount <= 0 || item == ItemType.None)
            return;

        SetItemCount(item, GetItemCount(item) + amount);

        Debug.Log($"[Inventory][Server] {item} +{amount}");
    }


    // ==========================================
    // 원소 포인트 지급
    // ==========================================

    [Server]
    public void AddElementPoints(ElementType element, int amount)
    {
        if (amount <= 0)
            return;

        SetElementPoints(
            element,
            GetElementPoints(element) + amount
        );

        Debug.Log($"[Inventory][Server] {element} +{amount}p");
    }


    // ==========================================
    // 원소 포인트 차감
    // ==========================================

    [Server]
    public bool TrySpendElementPoints(ElementType element, int amount)
    {
        if (amount <= 0)
            return false;

        int current = GetElementPoints(element);

        if (current < amount)
            return false;

        SetElementPoints(element, current - amount);

        Debug.Log($"[Inventory][Server] {element} -{amount}p");

        return true;
    }


    // ==========================================
    // 아이템 사용
    // ==========================================

    [Server]
    public bool TryUseItem(ItemType item)
    {
        if (item == ItemType.None)
            return false;

        int current = GetItemCount(item);

        if (current <= 0)
            return false;

        SetItemCount(item, current - 1);

        Debug.Log(
            $"[Inventory][Server] {item} 사용! 남은 수량: {current - 1}"
        );

        return true;
    }


    // ==========================================
    // 아이템 수량 조회
    // ==========================================

    public int GetItemCount(ItemType item)
    {
        switch (item)
        {
            case ItemType.Smoke:
                return smokeCount;

            case ItemType.Invisibility:
                return invisibilityCount;

            case ItemType.VineWall:
                return vineWallCount;

            case ItemType.WaterWall:
                return waterWallCount;

            case ItemType.FireWall:
                return fireWallCount;

            case ItemType.Bomb:
                return bombCount;

            default:
                return 0;
        }
    }


    // ==========================================
    // 원소 포인트 조회
    // ==========================================

    public int GetElementPoints(ElementType element)
    {
        switch (element)
        {
            case ElementType.Water:
                return waterPoints;

            case ElementType.Fire:
                return firePoints;

            case ElementType.Grass:
                return grassPoints;

            case ElementType.Wind:
                return windPoints;

            default:
                return 0;
        }
    }


    // ==========================================
    // 아이템 수량 변경 (서버 전용)
    // ==========================================

    [Server]
    private void SetItemCount(ItemType item, int count)
    {
        count = Mathf.Max(0, count);

        switch (item)
        {
            case ItemType.Smoke:
                smokeCount = count;
                break;

            case ItemType.Invisibility:
                invisibilityCount = count;
                break;

            case ItemType.VineWall:
                vineWallCount = count;
                break;

            case ItemType.WaterWall:
                waterWallCount = count;
                break;

            case ItemType.FireWall:
                fireWallCount = count;
                break;

            case ItemType.Bomb:
                bombCount = count;
                break;

            default:
                return;
        }

        InventoryChanged?.Invoke();
    }


    // ==========================================
    // 원소 포인트 변경 (서버 전용)
    // ==========================================

    [Server]
    private void SetElementPoints(ElementType element, int points)
    {
        points = Mathf.Max(0, points);

        switch (element)
        {
            case ElementType.Water:
                waterPoints = points;
                break;

            case ElementType.Fire:
                firePoints = points;
                break;

            case ElementType.Grass:
                grassPoints = points;
                break;

            case ElementType.Wind:
                windPoints = points;
                break;

            default:
                return;
        }

        InventoryChanged?.Invoke();
    }


    // ==========================================
    // SyncVar 변경 이벤트
    // ==========================================

    private void OnSmokeCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnInvisibilityCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnVineWallCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnWaterWallCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnFireWallCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnBombCountChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnWaterPointsChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnFirePointsChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnGrassPointsChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();

    private void OnWindPointsChanged(int oldValue, int newValue)
        => InventoryChanged?.Invoke();
}
