
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryTest : NetworkBehaviour
{
    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        // 자기 로컬 플레이어만 입력 처리
        if (!isLocalPlayer || Keyboard.current == null)
            return;

        // 1 키: 물 포인트 +1
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            inventory.AddElementPoints(ElementType.Water, 1);
        }

        // 2 키: 불 포인트 +1
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            inventory.AddElementPoints(ElementType.Fire, 1);
        }

        // 3 키: 물 포인트 -1
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            inventory.AddElementPoints(ElementType.Grass, 1);
        }

        // 4 키: 폭탄 +1
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            inventory.AddElementPoints(ElementType.Wind, 1);
        }

    }
}
