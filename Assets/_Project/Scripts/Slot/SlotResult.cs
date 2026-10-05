using System;

[Serializable]
public struct SlotResult
{
    // 플레이어별 슬롯 실행 회차입니다.
    public uint SpinId;

    // 왼쪽 릴의 최종 원소입니다.
    public ElementType First;

    // 가운데 릴의 최종 원소입니다.
    public ElementType Second;

    // 오른쪽 릴의 최종 원소입니다.
    public ElementType Third;

    // 슬롯 한 회차의 결과를 생성합니다.
    public SlotResult(uint spinId, ElementType first, ElementType second, ElementType third)
    {
        SpinId = spinId;
        First = first;
        Second = second;
        Third = third;
    }
}