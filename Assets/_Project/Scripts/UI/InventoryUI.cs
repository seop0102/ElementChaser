
using Mirror;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    /*[Header("아이템 UI")]
    [SerializeField] private TMP_Text smokeText;
    [SerializeField] private TMP_Text invisibilityText;
    [SerializeField] private TMP_Text vineWallText;
    [SerializeField] private TMP_Text waterWallText;
    [SerializeField] private TMP_Text fireWallText;
    [SerializeField] private TMP_Text bombText; */

    [Header("원소 포인트 UI")]
    [SerializeField] private TMP_Text waterPointsText;
    [SerializeField] private TMP_Text firePointsText;
    [SerializeField] private TMP_Text grassPointsText;
    [SerializeField] private TMP_Text windPointsText;

    private PlayerInventory inventory;

    private void OnEnable()
    {
        TryConnectInventory();
    }

    private void Update()
    {
        // 플레이어 생성 전에 UI가 실행될 수 있으므로
        // 연결되지 않았다면 다시 시도
        if (inventory == null)
        {
            TryConnectInventory();
        }
    }

    private void TryConnectInventory()
    {
        if (NetworkClient.localPlayer == null)
            return;

        PlayerInventory localInventory =
            NetworkClient.localPlayer.GetComponent<PlayerInventory>();

        if (localInventory == null)
            return;

        if (inventory == localInventory)
            return;

        // 이전 연결 해제
        DisconnectInventory();

        inventory = localInventory;

        // 인벤토리 변경 이벤트 구독
        inventory.InventoryChanged += RefreshUI;

        // 처음 연결할 때도 UI 갱신
        RefreshUI();

        Debug.Log("[InventoryUI] 로컬 인벤토리 연결 완료");
    }

    private void RefreshUI()
    {
        if (inventory == null)
            return;

        // 아이템 수량 표시
        /*smokeText.text =
            $"연막 x{inventory.GetItemCount(ItemType.Smoke)}";

        invisibilityText.text =
            $"투명화 x{inventory.GetItemCount(ItemType.Invisibility)}";

        vineWallText.text =
            $"덩굴 벽 x{inventory.GetItemCount(ItemType.VineWall)}";

        waterWallText.text =
            $"수벽 x{inventory.GetItemCount(ItemType.WaterWall)}";

        fireWallText.text =
            $"화염벽 x{inventory.GetItemCount(ItemType.FireWall)}";

        bombText.text =
            $"폭탄 x{inventory.GetItemCount(ItemType.Bomb)}"; */

        // 원소 포인트 표시
        waterPointsText.text =
            $"Water: {inventory.GetElementPoints(ElementType.Water)}p";

        firePointsText.text =
            $"Fire: {inventory.GetElementPoints(ElementType.Fire)}p";

        grassPointsText.text =
            $"Grass: {inventory.GetElementPoints(ElementType.Grass)}p";

        windPointsText.text =
            $"Wind: {inventory.GetElementPoints(ElementType.Wind)}p";
    }

    private void DisconnectInventory()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshUI;
            inventory = null;
        }
    }

    private void OnDisable()
    {
        DisconnectInventory();
    }

    private void OnDestroy()
    {
        DisconnectInventory();
    }
}
