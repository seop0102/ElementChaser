
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowableInput : NetworkBehaviour
{
    [Header("투척 시스템")]
    [SerializeField] private ThrowableItemUseController itemController;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private Camera playerCamera;

    private void Awake()
    {
        if (itemController == null)
            itemController = GetComponent<ThrowableItemUseController>();
    }

    private void Update()
    {
        // 자기 플레이어만 입력 처리
        if (!isLocalPlayer)
            return;

        if (itemController == null ||
            throwOrigin == null ||
            playerCamera == null)
            return;

        // 아이템 선택 중일 때만 입력 처리
        if (!itemController.HasSelection)
            return;

        Vector3 origin = throwOrigin.position;
        Vector3 direction = playerCamera.transform.forward;

        // 조준 경로 갱신
        itemController.UpdateAim(origin, direction);

        // 마우스 왼쪽 클릭: 투척
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            itemController.RequestThrow(origin, direction);
        }

        // 마우스 오른쪽 클릭: 취소
        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            itemController.CancelSelection();
        }
    }
}
