using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private float verticalVelocity;

    private bool isGameScene;
    private bool spawnReady;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        isGameScene = newScene.name == "GameScene";
        spawnReady = false;

        if (isGameScene && isServer)
            StartCoroutine(MoveToSpawnPoint());
    }

    public override void OnStartServer()
    {
        if (SceneManager.GetActiveScene().name == "GameScene")
            StartCoroutine(MoveToSpawnPoint());
    }

    private IEnumerator MoveToSpawnPoint()
    {
        yield return null;

        GameObject spawnPoint = GameObject.Find("SpawnPoint04");

        if (spawnPoint == null)
        {
            Debug.LogError("SpawnPoint04를 찾을 수 없음!");
            yield break;
        }

        Vector3 spawnPosition = spawnPoint.transform.position;
        spawnPosition.y += 2f;
        Quaternion spawnRotation = spawnPoint.transform.rotation;

        Vector3 rayOrigin = spawnPosition + Vector3.up * 5f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
        {
            Debug.Log($"바닥 감지 성공! {hit.collider.name}, 위치: {hit.point}");
        }
        else
        {
            Debug.LogError($"스폰 위치 아래에 바닥 없음! {spawnPosition}");
        }

        controller.enabled = false;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        verticalVelocity = 0f;

        controller.enabled = true;

        RpcSetSpawnPosition(spawnPosition, spawnRotation);

        spawnReady = true;

        Debug.Log($"SpawnPoint04로 이동 완료: {transform.position}");
    }

    [ClientRpc]
    private void RpcSetSpawnPosition(Vector3 position, Quaternion rotation)
    {
        if (isServer)
            return;

        controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        verticalVelocity = 0f;

        controller.enabled = true;

        spawnReady = true;
    }

    void Update()
    {
        isGameScene =
            SceneManager.GetActiveScene().name == "GameScene";

        if (!isLocalPlayer || !isGameScene || !spawnReady)
            return;

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

        direction.Normalize();

        if(controller.isGrounded && verticalVelocity < 0f)
    verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            direction * moveSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}