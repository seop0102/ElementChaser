
using System;
using Mirror;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("아이템 보유 수량")]
    [SerializeField, Min(0)] private int smokeCount = 0;
    [SerializeField, Min(0)] private int invisibilityCount = 0;
    [SerializeField, Min(0)] private int vineWallCount = 0;
    [SerializeField, Min(0)] private int waterWallCount = 0;
    [SerializeField, Min(0)] private int fireWallCount = 0;
    [SerializeField, Min(0)] private int bombCount = 0;

    [Header("원소 포인트")]
    [SerializeField, Min(0)] private int waterPoints = 0;
    [SerializeField, Min(0)] private int firePoints = 0;
    [SerializeField, Min(0)] private int grassPoints = 0;
    [SerializeField, Min(0)] private int windPoints = 0;

    // 인벤토리 데이터가 변경되면 UI에 알림
    public event Action InventoryChanged;

    // 아이템 지급
    public void AddItem(ItemType item, int amount = 1)
    {
        if (!isLocalPlayer || amount <= 0)
            return;

        SetItemCount(item, GetItemCount(item) + amount);

        InventoryChanged?.Invoke();

        Debug.Log($"[Inventory] {item} +{amount}");
    }

    // 원소 포인트 지급
    public void AddElementPoints(ElementType element, int amount)
    {
        if (!isLocalPlayer || amount <= 0)
            return;

        SetElementPoints(
            element,
            GetElementPoints(element) + amount
        );

        InventoryChanged?.Invoke();

        Debug.Log($"[Inventory] {element} +{amount}p");
    }

    // 원소 포인트 차감
    public bool TrySpendElementPoints(ElementType element, int amount)
    {
        if (!isLocalPlayer || amount <= 0)
            return false;

        int current = GetElementPoints(element);

        if (current < amount)
            return false;

        SetElementPoints(element, current - amount);

        InventoryChanged?.Invoke();

        Debug.Log($"[Inventory] {element} -{amount}p");

        return true;
    }

    // 아이템 1개 사용
    public bool TryUseItem(ItemType item)
    {
        if (!isLocalPlayer || item == ItemType.None)
            return false;

        int current = GetItemCount(item);

    // 아이템이 없으면 사용 실패
        if (current <= 0)
            return false;

    // 아이템 1개 차감
        SetItemCount(item, current - 1);

        InventoryChanged?.Invoke();

        Debug.Log($"[Inventory] {item} 사용! 남은 수량: {current - 1}");

        return true;
    }

    // 아이템 수량 조회
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

    // 원소 포인트 조회
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

    // 아이템 수량 변경
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
        }
    }

    // 원소 포인트 변경
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
        }
    }
}
