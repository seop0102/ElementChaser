
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
            CmdRequestAddElementPoints(ElementType.Water);
        }

        // 2 키: 불 포인트 +1
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            CmdRequestAddElementPoints(ElementType.Fire);
        }

        // 3 키: 물 포인트 +1
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            CmdRequestAddElementPoints(ElementType.Grass);
        }

        // 4 키: 바람 포인트 +1
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            CmdRequestAddElementPoints(ElementType.Wind);
        }

    }

    [Command]
    private void CmdRequestAddElementPoints(ElementType element)
    {
        if (inventory == null)
            return;

        inventory.AddElementPoints(element, 1);
    }

}
