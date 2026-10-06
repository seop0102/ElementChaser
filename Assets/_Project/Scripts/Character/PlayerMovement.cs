using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        if (!isLocalPlayer) return;
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
        }

        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        direction = direction.normalized;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }
}