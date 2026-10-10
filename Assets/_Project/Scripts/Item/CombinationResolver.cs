using System;
using UnityEngine;
// 조합표 조회-결과 반환
public class CombinationResolver
{
    private readonly CombinationTable _table;   //조합표
    public CombinationResolver()
    {
        this._table = Resources.Load<CombinationTable>("DefaultCombinationTable");

        if (this._table == null)
        {
            throw new InvalidOperationException("Resources/DefaultCombinationTable.asset을 찾을 수 없습니다.");
        }
    }

    // slot에서 호출할 api 
    public CombinationResult Evaluate(ElementType first, ElementType second, ElementType third)
    {
        if (!IsMaterial(first) || !IsMaterial(second) || !IsMaterial(third))
        {
            return CombinationResult.NoMatch;
        }

        CombinationEntry matchedEntry = null;

        foreach (CombinationEntry entry in this._table.Combinations)
        {
            if (entry == null)
            {
                throw new InvalidOperationException("조합표-비어 있는 행 존재");
            }

            if (!Matches(entry, first, second, third))
            {
                continue; // 일치하지 않으면 넘김
            }

            if (matchedEntry != null)
            {
                throw new InvalidOperationException("조합표-조합 중복 등록");
            }

            matchedEntry = entry;
        }

        if (matchedEntry == null)
        {
            return CombinationResult.NoMatch;
        }

        return CreateResult(matchedEntry);
    }

    //slot 3원소의 각 물,바람,풀,바람 수 == 조합표 한줄(식)의 각 물,바람,풀,바람 수 판별
    private static bool Matches(CombinationEntry entry, ElementType first, ElementType second, ElementType third)
    {
        return
            Count(ElementType.Water, entry.First, entry.Second, entry.Third)
                == Count(ElementType.Water, first, second, third)
            && Count(ElementType.Fire, entry.First, entry.Second, entry.Third)
                == Count(ElementType.Fire, first, second, third)
            && Count(ElementType.Grass, entry.First, entry.Second, entry.Third)
                == Count(ElementType.Grass, first, second, third)
            && Count(ElementType.Wind, entry.First, entry.Second, entry.Third)
                == Count(ElementType.Wind, first, second, third);
    }

    private static int Count(ElementType target, ElementType first, ElementType second, ElementType third)
    {
        int count = 0;

        if (first == target) count++;
        if (second == target) count++;
        if (third == target) count++;

        return count;
    }

    private static bool IsMaterial(ElementType element)
    {   //어떤 원소인가
        return element == ElementType.Water
            || element == ElementType.Fire
            || element == ElementType.Grass
            || element == ElementType.Wind;
    }

    private static CombinationResult CreateResult(CombinationEntry entry)
    {
        if (entry.Amount <= 0)
        {
            throw new InvalidOperationException("조합 보상의 지급량이 1보다 작습니다.");
        }

        switch (entry.RewardType)
        {
            case CombinationRewardType.Item:
                if (entry.Item == ItemType.None || !Enum.IsDefined(typeof(ItemType), entry.Item))
                {   //아이템인데 none이거나 정의되지 않은 아이템이면
                    throw new InvalidOperationException("아이템 보상의 아이템 종류가 올바르지 않습니다.");
                }

                return CombinationResult.ForItem(entry.Item, entry.Amount);

            case CombinationRewardType.Element:
                if (!IsMaterial(entry.Element))
                {   //원소인데 물,불,풀,바람이 아니면
                    throw new InvalidOperationException("원소 보상의 원소 종류가 올바르지 않습니다.");
                }

                return CombinationResult.ForElement(
                    entry.Element, entry.Amount);

            default:
                throw new InvalidOperationException("조합표의 보상 종류가 올바르지 않습니다.");
        }
    }
}