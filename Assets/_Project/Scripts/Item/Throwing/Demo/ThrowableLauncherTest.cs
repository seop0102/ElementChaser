using UnityEngine;

// 검증 전용: 투척 아이템 사용 API 호출
public sealed class ThrowableLauncherTest : MonoBehaviour
{
    [SerializeField] private ThrowableItemUseController _useController;
    [SerializeField] private ThrowableItemDefinition _itemDefinition;
    [SerializeField] private Transform _aim;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private Collider _testFloor;
    [SerializeField] private bool _allowLanding = true;
    [SerializeField, Min(0f)] private float _landingRadius = 3f;

    [ContextMenu("Select Test Item")]
    public void SelectTestItem()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (this._useController == null || this._itemDefinition == null
            || this._aim == null || this._testFloor == null)
        {
            Debug.LogWarning("사용 컨트롤러, 아이템 정의, 조준점, 바닥을 연결하세요.", this);
            return;
        }

        // TODO(협업: 구형님): 고정 아이템을 인벤토리 선택 결과로 교체
        this._useController.SelectItem(
            this._itemDefinition,
            this._landingRadius,
            this._collisionMask,
            this.IsLandingSurface,
            this.CanUseAtLanding);

        this.UpdatePreview();
    }

    [ContextMenu("Throw Test Item")]
    public void ThrowTestItem()
    {
        if (!Application.isPlaying || this._useController == null
            || this._aim == null || !this._useController.HasSelection)
        {
            return;
        }

        // TODO(협업: 지은님): 메뉴와 테스트 Transform을 실제 입력·조준으로 교체
        // TODO(협업: 채현님): 실제 게임에서는 서버 승인 흐름에 연결
        bool hasLaunched = this._useController.TryThrow(
            this._aim.position, this._aim.forward);

        Debug.Log(hasLaunched ? "[투척 검증] 발사" : "[투척 검증] 거절", this);
    }

    [ContextMenu("Cancel Test Item")]
    public void CancelTestItem()
    {
        if (Application.isPlaying && this._useController != null)
        {
            this._useController.CancelSelection();
        }
    }

    private void LateUpdate()
    {
        this.UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (this._useController == null || this._aim == null
            || !this._useController.HasSelection)
        {
            return;
        }

        // TODO(협업: 지은님): 테스트 Transform을 실제 조준 데이터로 교체
        this._useController.UpdateAim(this._aim.position, this._aim.forward);
    }

    // TODO(협업: 구형님): 테스트 바닥을 실제 타일 윗면 판정으로 교체
    private bool IsLandingSurface(RaycastHit hit)
    {
        return hit.collider == this._testFloor
            && Vector3.Dot(hit.normal, Vector3.up) >= 0.9f;
    }

    // TODO(협업: 구형님): 체크박스를 실제 맵 사용 가능 판정으로 교체
    private bool CanUseAtLanding(RaycastHit hit)
    {
        return this._allowLanding;
    }
}