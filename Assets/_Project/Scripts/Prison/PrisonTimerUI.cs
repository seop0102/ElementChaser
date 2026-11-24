using TMPro;
using UnityEngine;

// 감옥 위 월드 공간에 남은 시간을 표시한다.
[RequireComponent(typeof(NetworkPrison))]
public class PrisonTimerUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Canvas timerCanvas;
    [SerializeField] private RectTransform timerPanel;
    [SerializeField] private TMP_Text timeText;

    [Header("표시 설정")]
    [SerializeField, Min(0f)] private float heightOffset = 0.7f;
    [SerializeField, Min(0f)] private float warningSeconds = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    [Header("월드 UI 크기")]
    [SerializeField, Min(1f)] private float referenceDistance = 10f;
    [SerializeField, Min(0.001f)] private float referenceScale = 0.01f;

    private NetworkPrison prison;
    private Camera viewCamera;

    private void Awake()
    {
        prison = GetComponent<NetworkPrison>();

        // 필수 UI 연결이 빠졌으면 표시 기능을 중단한다.
        if (timerCanvas == null ||
            timerPanel == null ||
            timeText == null)
        {
            Debug.LogError(
                "PrisonTimerUI: UI 참조를 모두 연결해 주세요.",
                this
            );

            if (timerCanvas != null)
                timerCanvas.enabled = false;

            enabled = false;
            return;
        }

        // UI를 실제 월드 공간에 표시한다.
        timerCanvas.renderMode = RenderMode.WorldSpace;

        // 숫자 패널은 Canvas 가운데에 배치한다.
        timerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        timerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        timerPanel.pivot = new Vector2(0.5f, 0.5f);
        timerPanel.anchoredPosition3D = Vector3.zero;

        timerCanvas.enabled = false;
    }

    private void LateUpdate()
    {
        GameStateManager game = GameStateManager.Instance;

        // 감옥 착지 후, 게임 진행 중에만 표시한다.
        if (!prison.isClient ||
            prison.IsDropping ||
            prison.CageSize.y <= 0f ||
            game == null ||
            game.CurrentPhase != GamePhase.Playing)
        {
            timerCanvas.enabled = false;
            return;
        }

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
        {
            timerCanvas.enabled = false;
            return;
        }

        // 감옥 천장 위에 여백을 두고 배치한다.
        Vector3 worldPosition =
            prison.GroundPosition +
            Vector3.up * (prison.CageSize.y + heightOffset);

        // 카메라 정면 방향을 기준으로 깊이를 계산한다.
        float depth = Vector3.Dot(
            worldPosition - viewCamera.transform.position,
            viewCamera.transform.forward
        );

        // 카메라 뒤에 있는 숫자는 숨긴다.
        if (depth <= 0f)
        {
            timerCanvas.enabled = false;
            return;
        }

        Transform canvasTransform = timerCanvas.transform;

        canvasTransform.position = worldPosition;

        // 어느 방향에서 보더라도 숫자가 카메라를 향한다.
        canvasTransform.rotation = viewCamera.transform.rotation;

        float scale;

        if (viewCamera.orthographic)
        {
            // 직교 카메라는 줌 크기에 맞춰 보정한다.
            scale = referenceScale *
                (viewCamera.orthographicSize / 5f);
        }
        else
        {
            // 원근 카메라는 깊이와 시야각에 맞춰 보정한다.
            float fovRatio =
                Mathf.Tan(
                    viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad
                ) /
                Mathf.Tan(60f * 0.5f * Mathf.Deg2Rad);

            scale = referenceScale *
                (depth / referenceDistance) *
                fovRatio;
        }

        canvasTransform.localScale = Vector3.one * scale;

        // 남은 시간을 올림하여 정수로 표시한다.
        double remaining = prison.RemainingTime;
        timeText.text =
            Mathf.CeilToInt((float)remaining).ToString();

        // 10초 이하부터 빨간색으로 표시한다.
        timeText.color = remaining <= warningSeconds
            ? warningColor
            : normalColor;

        timerCanvas.enabled = true;
    }
}