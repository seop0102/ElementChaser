public readonly struct CombinationResult
{   // 조합 판정 결과
    public CombinationRewardType RewardType { get; }
    public ItemType Item { get; }
    public ElementType Element { get; }
    public int Amount { get; }

    // 보상 종류 None -> 실패
    public bool Success => this.RewardType != CombinationRewardType.None;

    private CombinationResult(CombinationRewardType rewardType, ItemType item, ElementType element, int amount)
    {
        this.RewardType = rewardType;
        this.Item = item;
        this.Element = element;
        this.Amount = amount;
    }

    // 매치x
    public static CombinationResult NoMatch => default;

    // 아이템 조합 성공
    public static CombinationResult ForItem(ItemType item, int amount)
    {
        return new CombinationResult(CombinationRewardType.Item, item, ElementType.Normal,amount);
    }

    // 원소 조합 성공
    public static CombinationResult ForElement(ElementType element, int points)
    {
        return new CombinationResult(CombinationRewardType.Element, ItemType.None, element, points);
    }
}