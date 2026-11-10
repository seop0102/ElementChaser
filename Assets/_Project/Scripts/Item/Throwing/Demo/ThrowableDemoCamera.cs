using UnityEngine;
using UnityEngine.InputSystem;

// 시연 전용: 카메라 이동·조준과 테스트 투척 입력
public sealed class ThrowableDemoCamera : MonoBehaviour
{
    [SerializeField] private ThrowableLauncherTest _launcherTest;
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _lookSensitivity = 0.15f;

    private float _yaw;
    private float _pitch;

    private void OnEnable()
    {
        Vector3 angles = this.transform.eulerAngles;
        this._yaw = angles.y;
        this._pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), -85f, 85f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            this.ReleaseCursor();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            return;
        }

        Vector2 look = mouse.delta.ReadValue();
        this._yaw += look.x * this._lookSensitivity;
        this._pitch = Mathf.Clamp(
            this._pitch - look.y * this._lookSensitivity, -85f, 85f);

        Quaternion heading = Quaternion.Euler(0f, this._yaw, 0f);
        this.transform.rotation = Quaternion.Euler(this._pitch, this._yaw, 0f);

        float x = (keyboard.dKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed ? 1f : 0f);
        float z = (keyboard.wKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed ? 1f : 0f);

        Vector3 move = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
        this.transform.position += heading * move * this._moveSpeed * Time.deltaTime;

        if (this._launcherTest == null)
        {
            return;
        }


        if (mouse.rightButton.wasPressedThisFrame)
        {
            this._launcherTest.CancelTestItem();
        }
        else if (keyboard.fKey.wasPressedThisFrame)
        {
            this._launcherTest.SelectTestItem();
        }
        else if (mouse.leftButton.wasPressedThisFrame)
        {
            this._launcherTest.ThrowTestItem();
        }
    }

    private void OnDisable()
    {
        this.ReleaseCursor();
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}