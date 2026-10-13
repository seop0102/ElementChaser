using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerLook : NetworkBehaviour
{
    public float mouseSensitivity = 0.1f;

    private Transform playerBody;
    private Camera playerCamera;
    private AudioListener audioListener;
    private float xRotation = 0f;

    private bool isGameScene = false;

    void Awake()
    {
        playerBody = transform.parent;
        playerCamera = GetComponent<Camera>();
        audioListener = GetComponent<AudioListener>();

        // 네트워크 플레이어가 활성화되기 전에는 카메라 비활성화
        playerCamera.enabled = false;

        if (audioListener != null)
            audioListener.enabled = false;
    }

    public override void OnStartLocalPlayer()
    {
        UpdateCameraState();
    }

    public override void OnStartClient()
    {
        UpdateCameraState();
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateCameraState();
    }

    void UpdateCameraState()
    {
        isGameScene =
            SceneManager.GetActiveScene().name == "GameScene";

        bool enableFPS = isLocalPlayer && isGameScene;

        if (playerCamera != null)
            playerCamera.enabled = enableFPS;

        if (audioListener != null)
            audioListener.enabled = enableFPS;

        // 커서는 로컬 플레이어만 제어
        if (isLocalPlayer)
        {
            // 게임 종료 시에는 커서를 표시
            bool shouldLock =
                enableFPS &&
                GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentPhase != GamePhase.Finished;

            Cursor.lockState = shouldLock
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !shouldLock;
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
            return;

        // 게임 종료 시 마우스 잠금 해제 및 카메라 회전 중지
        if (gameState.CurrentPhase == GamePhase.Finished)
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = true;
            return;
        }

        // 게임 진행 중에만 카메라 회전 허용
        if (gameState.CurrentPhase != GamePhase.Playing)
            return;

        // FPS 마우스 잠금
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }
}
