using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : NetworkBehaviour
{
    public float mouseSensitivity = 0.1f;

    private Transform playerBody;
    private Camera playerCamera;
    private AudioListener audioListener;
    private float xRotation = 0f;

    void Awake()
    {
        playerBody = transform.parent;

        playerCamera = GetComponent<Camera>();
        audioListener = GetComponent<AudioListener>();
    }

    public override void OnStartLocalPlayer()
    {
        playerCamera.enabled = true;

        if (audioListener != null)
            audioListener.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            playerCamera.enabled = false;

            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

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