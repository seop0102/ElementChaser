using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    
[Header("기본 이동")]
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private float gravity = -9.81f;

[Header("역할별 이동 속도")]
[SerializeField] private float chaserSpeedMultiplier = 1.2f;

[Header("달리기")]
[SerializeField] private float sprintMultiplier = 1.5f;

private PlayerRoleSync roleSync;

[Header("발소리")]
[SerializeField] private AudioSource footstepAudio;

private bool footstepsMoving;
private bool footstepsSprinting;



    private CharacterController controller;
    private float verticalVelocity;
    private Animator animator;
    private PlayerAnimationSync animationSync;

    private bool isGameScene;
    private bool spawnReady;
    private bool spawnAcknowledged;
    private PlayerTileController tileController;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        animationSync = GetComponent<PlayerAnimationSync>();
        tileController = GetComponent<PlayerTileController>();
        roleSync = GetComponent<PlayerRoleSync>(); 
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
        spawnAcknowledged = false;
        /*if (isGameScene && isServer)
            StartCoroutine(MoveToSpawnPoint());*/
    }
    /*
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
    */
    [Server]
    public void SetSpawnPosition(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);
        verticalVelocity = 0f;

        controller.enabled = true;
        spawnReady = true;

        if (connectionToClient != null)
        {
            TargetSetSpawnPosition(
                connectionToClient,
                position,
                rotation
            );
        }

        Debug.Log($"[Spawn] {netId} → {position}");
    }
    [TargetRpc]
    private void TargetSetSpawnPosition(
        NetworkConnectionToClient target,
        Vector3 position,
        Quaternion rotation)
    {
        controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);
        verticalVelocity = 0f;

        controller.enabled = true;
        spawnReady = true;

        Debug.Log($"[Spawn] 클라이언트 스폰 적용 완료: {netId}");

        if (!spawnAcknowledged)
        {
            spawnAcknowledged = true;
            CmdConfirmSpawn();
        }
    }
    [Command]
    private void CmdConfirmSpawn()
    {
        if (connectionToClient == null)
            return;

        ElementNetworkManager manager =
            NetworkManager.singleton as ElementNetworkManager;

        if (manager == null)
            return;

        manager.ConfirmPlayerSpawn(netId);
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

    // 로컬 플레이어, 게임 씬, 스폰 완료 확인
    if (!isLocalPlayer || !isGameScene || !spawnReady)
    {   
        SetFootsteps(false, false);
        return;
    }


    // 사망한 플레이어는 이동 불가능
    PlayerHealth health = GetComponent<PlayerHealth>();

    if (health != null && health.IsDead)
    {
        if (animationSync != null)
            animationSync.SetMoving(false);
        SetFootsteps(false, false);
        return;
    }

            // 게임 진행 중에만 이동 가능
        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
        {
            animationSync?.SetMoving(false);
            SetFootsteps(false, false);
            return;
        }

        PlayerPrison prison = GetComponent<PlayerPrison>();

        // 체력 동기화가 감금 정보보다 먼저 도착하는 짧은 순간도 막는다.
        bool waitingForPrison =
            health != null &&
            health.CurrentHealth <= 0 &&
            prison != null &&
            !prison.IsJailed;

        if (waitingForPrison || (prison != null && prison.IsDropping))
        {
            if (animationSync != null)
                animationSync.SetMoving(false);

            verticalVelocity = 0f;

            if (prison != null && prison.IsJailed)
            {
                Vector3 fixedPosition =
                    prison.ConstrainPosition(transform.position);

                controller.Move(fixedPosition - transform.position);
            }
            SetFootsteps(false, false);
            return;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
        }

// 술래일 경우 기본 이동 속도 증가
float currentSpeed = moveSpeed;

if (roleSync != null && roleSync.IsChaser)
{
    currentSpeed *= chaserSpeedMultiplier;
}

// Shift를 누르고 있으면 달리기
bool isSprinting =
    Keyboard.current != null &&
    (Keyboard.current.leftShiftKey.isPressed ||
     Keyboard.current.rightShiftKey.isPressed);

if (isSprinting && input.sqrMagnitude > 0.01f)
{
    currentSpeed *= sprintMultiplier;
}


        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        direction.Normalize();
        // X축과 Z축 이동 방향 분리
        Vector3 moveX = new Vector3(direction.x, 0f, 0f);
        Vector3 moveZ = new Vector3(0f, 0f, direction.z);
        // 이동 예정 위치의 타일 진입 가능 여부 확인
        if (tileController != null)
        {
            // 1. X축 이동 가능 여부 검사
            Vector3 xTarget =
                transform.position + moveX * currentSpeed * Time.deltaTime;
             if (!tileController.CanMoveTo(xTarget))
            {
                moveX = Vector3.zero;
            }
            // 2. X축 이동을 반영한 상태에서 Z축 검사
            Vector3 zTarget =
                transform.position +
                moveX * currentSpeed * Time.deltaTime +
                moveZ * currentSpeed * Time.deltaTime;

             if (!tileController.CanMoveTo(zTarget))
            {
                moveZ = Vector3.zero;
            }
            // 최종 이동 방향
            direction = moveX + moveZ;
        }

        // 실제 이동 가능 여부와 관계없이
        // WASD 입력이 있으면 Run 애니메이션 유지
        bool isTryingToMove = input.sqrMagnitude > 0.01f;

        if (animationSync != null)
        {
            animationSync.SetMoving(isTryingToMove);
        }
        else if (animator != null)
        {
            animator.SetFloat("Speed", isTryingToMove ? 1f : 0f);
        }

        if(controller.isGrounded && verticalVelocity < 0f)
    verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            direction * currentSpeed +
            Vector3.up * verticalVelocity;

        // 먼저 이동할 위치를 계산한다.
        Vector3 targetPosition =
            transform.position + velocity * Time.deltaTime;

        // 감금 중이라면 감옥 내부로 이동 범위를 제한한다.
        if (prison != null)
            targetPosition = prison.ConstrainPosition(targetPosition);

        
Vector3 beforePosition = transform.position;

controller.Move(targetPosition - transform.position);

// 실제 수평 이동 거리 확인
Vector3 actualMovement = transform.position - beforePosition;
actualMovement.y = 0f;

bool isActuallyMoving =
    actualMovement.sqrMagnitude > 0.000001f;

// 실제 이동 중일 때만 발소리
SetFootsteps(isActuallyMoving, isSprinting);

    }

private void SetFootsteps(bool moving, bool sprinting)
{
    sprinting = moving && sprinting;

    // 상태가 변하지 않았다면 네트워크 전송 생략
    if (footstepsMoving == moving &&
        footstepsSprinting == sprinting)
        return;

    footstepsMoving = moving;
    footstepsSprinting = sprinting;

    // 자기 발소리는 즉시 적용
    ApplyFootsteps(moving, sprinting);

    if (isLocalPlayer)
        CmdSetFootsteps(moving, sprinting);
}

private void ApplyFootsteps(bool moving, bool sprinting)
{
    if (footstepAudio == null)
        return;

    if (!moving)
    {
        footstepAudio.Stop();
        return;
    }

    // 달릴 때 발소리도 빨라짐
    footstepAudio.pitch = sprinting ? 1.35f : 1f;

    if (!footstepAudio.isPlaying)
        footstepAudio.Play();
}

[Command]
private void CmdSetFootsteps(bool moving, bool sprinting)
{
    RpcSetFootsteps(moving, sprinting);
}

[ClientRpc]
private void RpcSetFootsteps(bool moving, bool sprinting)
{
    // 자기 발소리는 이미 로컬에서 재생했으므로 중복 방지
    if (isLocalPlayer)
        return;

    ApplyFootsteps(moving, sprinting);
}

}