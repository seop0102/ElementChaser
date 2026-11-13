using UnityEngine;
using UnityEngine.InputSystem;

// 호스트 로컬 검증용 연막 장착
public sealed class LocalSmokeEquipTest : MonoBehaviour
{
    [SerializeField] private ThrowableItemDefinition _smokeDefinition;
    [SerializeField] private LayerMask _collisionMask;

    private PlayerRoleSync _playerRole;
    private ThrowableItemUseController _useController;

    private void Awake()
    {
        this._playerRole = GetComponent<PlayerRoleSync>();
        this._useController = GetComponent<ThrowableItemUseController>();
    }

    private void Update()   //임시 f8키로 연막 장착 테스트
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || !keyboard.f8Key.wasPressedThisFrame)
        {
            return;
        }

        GameStateManager gameState = GameStateManager.Instance;

        if (this._playerRole == null || !this._playerRole.isServer
            || !this._playerRole.isLocalPlayer || !this._playerRole.IsRunner
            || this._playerRole.IsJailed || gameState == null
            || gameState.CurrentPhase != GamePhase.Playing)
        {
            return;
        }

        if (this._smokeDefinition == null || this._useController == null)
        {
            Debug.LogWarning("연막 정의와 아이템 사용 컨트롤러를 연결하세요.", this);
            return;
        }

        // TODO(구형님 연동): 인벤토리 장착 결과로 교체
        this._useController.SelectItem(
            this._smokeDefinition,
            3f,
            this._collisionMask,
            IsTileTop,
            _ => true);
    }

    // TODO(구형님 연동): 실제 타일 윗면 판정으로 교체
    private static bool IsTileTop(RaycastHit hit)
    {
        return hit.collider != null
            && hit.collider.GetComponentInParent<Tile>() != null
            && Vector3.Dot(hit.normal, Vector3.up) >= 0.9f;
    }
}