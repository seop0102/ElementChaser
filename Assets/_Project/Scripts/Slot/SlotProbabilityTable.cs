using System;
using System.Collections.Generic;
using UnityEngine;

// 확률표에 등록되는 조합 한 개입니다.
[Serializable]
public class SlotCombinationEntry
{
    public ElementType First;
    public ElementType Second;
    public ElementType Third;

    // 0이면 추첨에서 제외됩니다.
    [Min(0f)]
    public float Weight;

    // 조합과 초기 가중치를 설정합니다.
    public SlotCombinationEntry(ElementType first, ElementType second, ElementType third, float weight)
    {
        First = first;
        Second = second;
        Third = third;
        Weight = weight;
    }
}

// Unity에서 설정 에셋으로 만드는 슬롯 확률표입니다.
[CreateAssetMenu(
    fileName = "SlotProbabilityTable",
    menuName = "ElementChaser/Slot/Probability Table")]
public class SlotProbabilityTable : ScriptableObject
{
    // 순서를 구분하지 않는 20개 조합을 모두 등록합니다.
    [SerializeField]
    private List<SlotCombinationEntry> _combinations =
        new List<SlotCombinationEntry>
        {
            // 물이 포함된 조합 10개입니다.
            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Water,
                ElementType.Water,
                1f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Water,
                ElementType.Fire,
                3f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Water,
                ElementType.Grass,
                3f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Water,
                ElementType.Wind,
                3f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Fire,
                ElementType.Fire,
                3f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Fire,
                ElementType.Grass,
                6f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Fire,
                ElementType.Wind,
                6f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Grass,
                ElementType.Grass,
                3f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Grass,
                ElementType.Wind,
                6f),

            new SlotCombinationEntry(
                ElementType.Water,
                ElementType.Wind,
                ElementType.Wind,
                3f),

            // 물 없이 불이 포함된 조합 6개입니다.
            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Fire,
                ElementType.Fire,
                1f),

            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Fire,
                ElementType.Grass,
                3f),

            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Fire,
                ElementType.Wind,
                3f),

            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Grass,
                ElementType.Grass,
                3f),

            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Grass,
                ElementType.Wind,
                6f),

            new SlotCombinationEntry(
                ElementType.Fire,
                ElementType.Wind,
                ElementType.Wind,
                3f),

            // 물과 불 없이 풀이 포함된 조합 3개입니다.
            new SlotCombinationEntry(
                ElementType.Grass,
                ElementType.Grass,
                ElementType.Grass,
                1f),

            new SlotCombinationEntry(
                ElementType.Grass,
                ElementType.Grass,
                ElementType.Wind,
                3f),

            new SlotCombinationEntry(
                ElementType.Grass,
                ElementType.Wind,
                ElementType.Wind,
                3f),

            // 바람만 있는 조합 1개입니다.
            new SlotCombinationEntry(
                ElementType.Wind,
                ElementType.Wind,
                ElementType.Wind,
                1f)
        };

    // 추첨 로직에서 조합 목록을 읽습니다.
    public IReadOnlyList<SlotCombinationEntry> Combinations => _combinations;

    // 현재 설정된 가중치의 합계를 계산합니다.
    public float TotalWeight
    {
        get
        {
            float total = 0f;

            foreach (SlotCombinationEntry entry in _combinations)
            {
                if (entry != null)
                {
                    total += entry.Weight;
                }
            }

            return total;
        }
    }

    // 특정 행의 현재 확률을 백분율로 반환합니다.
    public float GetProbabilityPercent(int index)
    {
        if (index < 0 || index >= _combinations.Count)
        {
            return 0f;
        }

        SlotCombinationEntry entry = _combinations[index];
        float total = TotalWeight;

        if (entry == null || total <= 0f)
        {
            return 0f;
        }

        return entry.Weight / total * 100f;
    }

    // Inspector에서 입력한 잘못된 가중치를 정리합니다.
    private void OnValidate()
    {
        foreach (SlotCombinationEntry entry in _combinations)
        {
            if (entry == null)
            {
                continue;
            }

            if (float.IsNaN(entry.Weight)
                || float.IsInfinity(entry.Weight)
                || entry.Weight < 0f)
            {
                entry.Weight = 0f;
            }
        }
    }
}