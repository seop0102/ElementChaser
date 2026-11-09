
using Mirror;
using UnityEngine;

public class PlayerTileController : NetworkBehaviour
{
    [Header("타일 감지 설정")]
    [SerializeField]
    private LayerMask tileLayer;

    [SerializeField]
    private float rayDistance = 3f;

    private PlayerInventory inventory;

    // 마지막으로 밟은 타일 오브젝트
    private Tile previousTile;

    private bool initialized = false;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void LateUpdate()
    {
        // 로컬 플레이어만 실행
        if (!isLocalPlayer)
            return;

        // 게임 진행 중에만 실행
        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
            return;

        CheckTile();
    }

    private void CheckTile()
    {
        Vector3 origin =
            transform.position + Vector3.up * 0.5f;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                tileLayer))
        {
            return;
        }

        // 기존 Tile.cs 활용
        Tile currentTile =
            hit.collider.GetComponentInParent<Tile>();

        if (currentTile == null)
            return;

        // 최초 감지 타일은 등록만 함
        if (!initialized)
        {
            previousTile = currentTile;
            initialized = true;
            return;
        }

        // 동일한 타일 오브젝트면 차감하지 않음
        if (previousTile == currentTile)
            return;

        // 새로운 타일로 이동한 경우
        previousTile = currentTile;

        ElementType element = currentTile.type;

        // 일반 타일은 차감하지 않음
        if (element == ElementType.Normal)
            return;

        // 타일에 대응하는 상쇄 원소 결정
        ElementType costElement = GetCounterElement(element);

       // 상쇄 원소 포인트 1 차감
        bool success =
            inventory.TrySpendElementPoints(costElement, 1);

        if (success)
        {
            Debug.Log(
                $"[Tile] {element} 타일 진입 → 1p 차감"
            );
        }
        else
        {
            Debug.Log(
                $"[Tile] {element} 포인트 부족"
            );
        }
    }

    private ElementType GetCounterElement(ElementType tileType)
    {
        switch (tileType)
        {
         case ElementType.Water:
                return ElementType.Grass;

            case ElementType.Fire:
                return ElementType.Water;

            case ElementType.Grass:
                return ElementType.Fire;

            case ElementType.Wind:
                return ElementType.Wind;

            default:
                return ElementType.Normal;
        }
    }

    public bool CanMoveTo(Vector3 targetPosition)
    {
        // 현재 위치와 이동 예정 위치의 타일 확인
        Tile currentTile = GetTileAtPosition(transform.position);
        Tile targetTile = GetTileAtPosition(targetPosition);

        // 타일이 감지되지 않으면 기존 이동 허용
        if (targetTile == null)
            return false;

        // 현재 밟고 있는 타일이면 포인트 검사 없이 이동 허용
        if (currentTile == targetTile)
            return true;

        // 일반 타일은 포인트 없이 이동 가능
        if (targetTile.type == ElementType.Normal)
            return true;

        // 필요한 상쇄 원소 확인
        ElementType costElement = GetCounterElement(targetTile.type);

        // 인벤토리가 없거나 포인트가 부족하면 이동 차단
        if (inventory == null)
            return false;

        return inventory.GetElementPoints(costElement) >= 1;
    }

    // 특정 위치 아래에 있는 타일 감지
    private Tile GetTileAtPosition(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 0.5f;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                tileLayer))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<Tile>();
    }
}
