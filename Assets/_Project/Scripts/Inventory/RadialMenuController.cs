
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using TMPro;
using UnityEngine.UI;


public class RadialMenuController : MonoBehaviour
{
    [Header("Radial Menu Items")]
    [SerializeField] private RectTransform[] itemImages;

    [Header("Layout Settings")]
    [SerializeField] private float radius = 170f;

    [Header("Menu Settings")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private RectTransform menuRect;

    [Header("Selection Settings")]
    [SerializeField] private float selectedScale = 1.3f;
    [SerializeField] private float minSelectDistance = 40f;
    [Header("Item UI")]
    [SerializeField] private TMP_Text selectedItemName;
    [SerializeField] private TMP_Text[] itemCountTexts;

    [SerializeField] private string[] itemNames =
    {
        "Smoke",
        "Invisibility",
        "Vine Wall",
        "Water Wall",
        "Fire Wall",
        "Bomb",
    };

    [SerializeField] private int[] itemCounts =
    {
        0, 0, 0, 0, 0, 0
    };
    private PlayerLook playerLook;
    private PlayerInventory playerInventory;

    private bool isOpen = false;
    private int selectedIndex = -1;

    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private ThrowableItemUseController throwableItemUseController;

    private void Start()
    {
        ArrangeItems();
        menuRoot.SetActive(false);
    }

    private void Update()
    {
        TryConnectInventory();
        if (Keyboard.current == null)
            return;

        if (!isOpen && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenMenu();
        }

        if (!isOpen)
            return;

        UpdateSelection();


        // E키를 놓으면 선택 확정
        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CloseMenu(true);
        }

        // 우클릭은 선택 취소
        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            CloseMenu(false);
        }
    }

    private void TryConnectInventory()
    {
        // 이미 연결되어 있으면 중복 연결 방지
        if (playerInventory != null)
            return;

        // 로컬 플레이어가 아직 없으면 다음 프레임에 재시도
        if (NetworkClient.localPlayer == null)
            return;

        GameObject localPlayer = NetworkClient.localPlayer.gameObject;
        playerInventory = localPlayer.GetComponent<PlayerInventory>();
         throwableItemUseController = localPlayer.GetComponent<ThrowableItemUseController>();

        if (playerInventory == null || throwableItemUseController == null)
            return;

        // 인벤토리 변경 이벤트 등록
        playerInventory.InventoryChanged += RefreshItemCounts;

        Debug.Log("[RadialMenu] Inventory 및 ThrowableController 연결 성공!");

        RefreshItemCounts();
    }

    private void ArrangeItems()
    {
        int count = itemImages.Length;

        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            float radian = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius;

            itemImages[i].anchoredPosition =
                new Vector2(x, y);
        }
    }

   private void OpenMenu()
    {
        if (isOpen) return;

        // 내 로컬 플레이어 찾기
        if (NetworkClient.localPlayer == null)
        {
            Debug.LogError("Local Player를 찾을 수 없습니다.");
            return;
        }

        playerLook = NetworkClient.localPlayer
            .GetComponentInChildren<PlayerLook>(true);

        if (playerLook == null)
        {
            Debug.LogError("PlayerLook 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        isOpen = true;
        selectedIndex = -1;

        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // 카메라 회전 잠금
        playerLook.isRadialMenuOpen = true;

        // 마우스 커서 활성화
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuRoot.SetActive(true);

        UpdateHighlights();
    }

    private void UpdateSelection()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Canvas canvas = menuRoot.GetComponentInParent<Canvas>();

        Camera eventCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            menuRect,
            mousePosition,
            eventCamera,
            out Vector2 localPosition))
            return;

        // 중앙에서 너무 가까우면 선택 해제
        if (localPosition.magnitude < minSelectDistance)
        {
            selectedIndex = -1;
        }
        else
        {
            // 위쪽을 0번으로 시작하여 시계 방향 계산
            float angle = Mathf.Atan2(
                localPosition.x,
                localPosition.y
            ) * Mathf.Rad2Deg;

            if (angle < 0)
                angle += 360f;

            selectedIndex =
                Mathf.RoundToInt(angle / 60f) % itemImages.Length;
        }

        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        for (int i = 0; i < itemImages.Length; i++)
        {
            bool isSelected = i == selectedIndex;
            bool hasItem = itemCounts[i] > 0;

            // 선택된 아이템 확대
            float scale = i == selectedIndex ? selectedScale : 1f;
            itemImages[i].localScale = Vector3.one * scale;

            // 수량이 0인 아이템만 어둡게 표시
            Image image = itemImages[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = hasItem ? Color.white
                    : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }

             // 아이템 보유 개수 표시
            if (itemCountTexts != null && i < itemCountTexts.Length)
            {
                itemCountTexts[i].text = itemCounts[i].ToString();
            }
        }

        // 중앙 아이템 이름 표시
        if (selectedItemName != null)
        {
            selectedItemName.text = selectedIndex >= 0 ? itemNames[selectedIndex]
                : "취소";
        }
    }

    private void CloseMenu(bool confirmSelection)
{
    if (!isOpen) return;

    isOpen = false;

    if (confirmSelection)
    {
        // 중앙에 위치하면 None, 아니면 선택한 아이템
        ItemType selectedItem = selectedIndex >= 0
            ? GetSelectedItemType()
            : ItemType.None;

        if (throwableItemUseController != null)
        {
            // 선택한 아이템 또는 None 장착 요청
            if (selectedItem == ItemType.None ||
                (playerInventory != null &&
                 playerInventory.GetItemCount(selectedItem) > 0))
            {
                throwableItemUseController.SetHeldItem(selectedItem);

                Debug.Log($"[RadialMenu] {selectedItem} 장착 요청");
            }
            else
            {
                Debug.Log("해당 아이템을 보유하고 있지 않습니다.");
            }
        }
    }

    menuRoot.SetActive(false);

    // 카메라 회전 복구
    if (playerLook != null)
        playerLook.isRadialMenuOpen = false;

    // 커서 상태 복구
    Cursor.lockState = previousLockMode;
    Cursor.visible = previousCursorVisible;

    selectedIndex = -1;
    UpdateHighlights();
}

    private void RefreshItemCounts()
    {
        if (playerInventory == null)
            return;

        itemCounts[0] = playerInventory.GetItemCount(ItemType.Smoke);
        itemCounts[1] = playerInventory.GetItemCount(ItemType.Invisibility);
        itemCounts[2] = playerInventory.GetItemCount(ItemType.VineWall);
        itemCounts[3] = playerInventory.GetItemCount(ItemType.WaterWall);
        itemCounts[4] = playerInventory.GetItemCount(ItemType.FireWall);
        itemCounts[5] = playerInventory.GetItemCount(ItemType.Bomb);
        //itemCounts[6] = playerInventory.GetItemCount(ItemType.None);

        UpdateHighlights();
    }

    private ItemType GetSelectedItemType()
    {
        switch (selectedIndex)
        {
            case 0: return ItemType.Smoke;
            case 1: return ItemType.Invisibility;
            case 2: return ItemType.VineWall;
            case 3: return ItemType.WaterWall;
            case 4: return ItemType.FireWall;
            case 5: return ItemType.Bomb;

            default: return ItemType.None;
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= RefreshItemCounts;
        }
    }
}
