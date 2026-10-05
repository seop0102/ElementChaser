using System;

// 확률표를 이용해 슬롯 결과를 추첨합니다.
public class SlotRoller
{
    // 각 슬롯이 사용할 확률표입니다.
    private readonly SlotProbabilityTable _probabilityTable;

    // 모든 SlotRoller가 공유하는 난수 생성기입니다.
    private static readonly Random _random = new Random();

    // 사용할 확률표와 난수 생성기를 준비합니다.
    public SlotRoller(SlotProbabilityTable probabilityTable)
    {
        if (probabilityTable == null)
        {
            throw new ArgumentNullException(nameof(probabilityTable));
        }

        _probabilityTable = probabilityTable;
    }

    // 조합을 추첨하고 표시 순서를 섞어 반환합니다.
    public SlotResult Roll(uint spinId)
    {
        double totalWeight = GetValidTotalWeight();

        SlotCombinationEntry combination = SelectCombination(totalWeight);

        ElementType[] elements = {
            combination.First,
            combination.Second,
            combination.Third
        };

        ShuffleElements(elements);

        return new SlotResult(
            spinId,
            elements[0],
            elements[1],
            elements[2]);
    }

    // 설정값을 검사하고 가중치 합계를 계산합니다.
    private double GetValidTotalWeight()
    {
        double totalWeight = 0d;

        foreach (SlotCombinationEntry combination in _probabilityTable.Combinations)
        {
            if (combination == null)
            {
                throw new InvalidOperationException(
                    "슬롯 확률표에 비어 있는 조합이 있습니다.");
            }

            float weight = combination.Weight;

            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
            {
                throw new InvalidOperationException(
                    "슬롯 가중치는 유한한 0 이상의 값이어야 합니다.");
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0d)
        {
            throw new InvalidOperationException(
                "슬롯 가중치 중 하나 이상은 0보다 커야 합니다.");
        }

        return totalWeight;
    }

    // 누적 가중치 구간에 해당하는 조합을 선택합니다.
    private SlotCombinationEntry SelectCombination(double totalWeight)
    {
        double randomValue = _random.NextDouble() * totalWeight;
        double cumulativeWeight = 0d;

        SlotCombinationEntry lastPositiveCombination = null;

        foreach (SlotCombinationEntry combination in _probabilityTable.Combinations)
        {
            // 가중치 0인 조합은 선택하지 않습니다.
            if (combination.Weight <= 0f)
            {
                continue;
            }

            lastPositiveCombination = combination;
            cumulativeWeight += combination.Weight;

            if (randomValue < cumulativeWeight)
            {
                return combination;
            }
        }

        // 부동소수점 계산 오차에 대비합니다.
        if (lastPositiveCombination != null)
        {
            return lastPositiveCombination;
        }

        throw new InvalidOperationException(
            "추첨할 수 있는 슬롯 조합이 없습니다.");
    }

    // Fisher–Yates 방식으로 표시 순서를 섞습니다.
    private void ShuffleElements(ElementType[] elements)
    {
        for (int index = elements.Length - 1; index > 0; index--)
        {
            int randomIndex = _random.Next(index + 1);

            ElementType temporaryElement = elements[index];
            elements[index] = elements[randomIndex];
            elements[randomIndex] = temporaryElement;
        }
    }
}