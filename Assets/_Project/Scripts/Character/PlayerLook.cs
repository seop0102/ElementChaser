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
            Cursor.lockState = enableFPS
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !enableFPS;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !isGameScene)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;
        
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

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}