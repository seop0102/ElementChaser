using System;
using UnityEngine;

[Serializable]
public class CombinationEntry
{
    [Header("입력 원소")]
    public ElementType First;
    public ElementType Second;
    public ElementType Third;

    [Header("보상 종류")]
    public CombinationRewardType RewardType;

    [Tooltip("아이템 보상일 때")]
    public ItemType Item;

    [Tooltip("원소 보상일 때")]
    public ElementType Element;

    [Tooltip("아이템 수/원소 p")]
    [Min(1)]
    public int Amount;

    public static CombinationEntry ForItem(
        ElementType first,
        ElementType second,
        ElementType third,
        ItemType item,
        int amount)
    {
        return new CombinationEntry
        {
            First = first,
            Second = second,
            Third = third,
            RewardType = CombinationRewardType.Item,
            Item = item,
            Element = ElementType.Normal, // 아이템은 원소 속성 x
            Amount = amount
        };
    }

    public static CombinationEntry ForElement(
        ElementType first,
        ElementType second,
        ElementType third,
        ElementType element,
        int points)
    {
        return new CombinationEntry
        {
            First = first,
            Second = second,
            Third = third,
            RewardType = CombinationRewardType.Element,
            Item = ItemType.None, // 원소p는 아이템 타입 x
            Element = element,
            Amount = points
        };
    }
}