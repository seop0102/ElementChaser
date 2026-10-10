using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DefaultCombinationTable",
    menuName = "ElementChaser/Item/Combination Table")]
public class CombinationTable : ScriptableObject
{

    private const int InitialItemAmount = 1;
    private const int InitialElementPoints = 5;

    [SerializeField]
    private List<CombinationEntry> _combinations =
        new List<CombinationEntry>
        {
            // 기획 10개 조합식 배치
            // 아이템 조합 6개
            CombinationEntry.ForItem(
                ElementType.Water, ElementType.Fire, ElementType.Wind,
                ItemType.Smoke, InitialItemAmount),

            CombinationEntry.ForItem(
                ElementType.Wind, ElementType.Wind, ElementType.Water,
                ItemType.Invisibility, InitialItemAmount),

            CombinationEntry.ForItem(
                ElementType.Grass, ElementType.Grass, ElementType.Water,
                ItemType.VineWall, InitialItemAmount),

            CombinationEntry.ForItem(
                ElementType.Water, ElementType.Water, ElementType.Wind,
                ItemType.WaterWall, InitialItemAmount),

            CombinationEntry.ForItem(
                ElementType.Fire, ElementType.Fire, ElementType.Grass,
                ItemType.FireWall, InitialItemAmount),

            CombinationEntry.ForItem(
                ElementType.Fire, ElementType.Grass, ElementType.Wind,
                ItemType.Bomb, InitialItemAmount),

            // 원소 조합 4개
            CombinationEntry.ForElement(
                ElementType.Water, ElementType.Water, ElementType.Water,
                ElementType.Water, InitialElementPoints),

            CombinationEntry.ForElement(
                ElementType.Fire, ElementType.Fire, ElementType.Fire,
                ElementType.Fire, InitialElementPoints),

            CombinationEntry.ForElement(
                ElementType.Grass, ElementType.Grass, ElementType.Grass,
                ElementType.Grass, InitialElementPoints),

            CombinationEntry.ForElement(
                ElementType.Wind, ElementType.Wind, ElementType.Wind,
                ElementType.Wind, InitialElementPoints)
        };

    // 조합 판정용 읽기
    public IReadOnlyList<CombinationEntry> Combinations => _combinations;
}