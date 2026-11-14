
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerSpectator : NetworkBehaviour
{
    private PlayerHealth health;
    private PlayerLook playerLook;
    private Camera playerCamera;

    private Transform originalCameraParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private readonly List<PlayerHealth> targets =
        new List<PlayerHealth>();

    private PlayerHealth currentTarget;
    private bool spectating;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        playerLook = GetComponentInChildren<PlayerLook>(true);
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            originalCameraParent = playerCamera.transform.parent;
            originalLocalPosition =
                playerCamera.transform.localPosition;
            originalLocalRotation =
                playerCamera.transform.localRotation;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        if (health == null)
            return;

        if (!health.IsDead)
        {
            if (spectating)
                StopSpectating();

            return;
        }

        if (!spectating)
            StartSpectating();

        RefreshTargets();

        if (currentTarget == null ||
            !targets.Contains(currentTarget))
        {
            SelectTarget(0);
        }

        if (Keyboard.current == null || targets.Count == 0)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            ChangeTarget(1);

        if (Keyboard.current.qKey.wasPressedThisFrame)
            ChangeTarget(-1);
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer || !spectating)
            return;

        if (currentTarget == null || playerCamera == null)
            return;

        PlayerLook targetLook =
            currentTarget.GetComponentInChildren<PlayerLook>(true);

        if (targetLook == null)
            return;

        Transform targetCamera = targetLook.transform;

        // 생존자의 카메라 위치 추적
        playerCamera.transform.position =
            targetCamera.position;

        // 생존자의 몸체 좌우 회전
        float yaw = currentTarget.transform.eulerAngles.y;

        // PlayerLook에서 동기화한 상하 회전
        float pitch = targetLook.SyncedPitch;

        playerCamera.transform.rotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }

    private void StartSpectating()
    {
        if (playerCamera == null)
            return;

        spectating = true;

        // 사망한 몸체의 영향을 받지 않도록 카메라 분리
        playerCamera.transform.SetParent(null, true);

        if (playerLook != null)
            playerLook.enabled = false;

        Debug.Log("[관전] 관전 모드 시작");
    }

    private void StopSpectating()
    {
        spectating = false;
        currentTarget = null;

        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(
                originalCameraParent, false);

            playerCamera.transform.localPosition =
                originalLocalPosition;

            playerCamera.transform.localRotation =
                originalLocalRotation;
        }

        if (playerLook != null)
            playerLook.enabled = true;

        Debug.Log("[관전] 관전 모드 종료");
    }

    private void RefreshTargets()
    {
        targets.Clear();

        foreach (NetworkIdentity identity
                 in NetworkClient.spawned.Values)
        {
            if (identity == null)
                continue;

            PlayerHealth candidate =
                identity.GetComponent<PlayerHealth>();

            if (candidate == null ||
                candidate == health ||
                candidate.IsDead)
                continue;

            targets.Add(candidate);
        }

        // 클라이언트마다 순서가 달라지지 않도록 정렬
        targets.Sort((a, b) =>
            a.netId.CompareTo(b.netId));
    }

    private void SelectTarget(int index)
    {
        if (targets.Count == 0)
        {
            currentTarget = null;
            return;
        }

        index = (index % targets.Count + targets.Count)
                % targets.Count;

        currentTarget = targets[index];

        Debug.Log(
            $"[관전] 대상 변경: {currentTarget.name}"
        );
    }

    private void ChangeTarget(int direction)
    {
        if (targets.Count == 0)
            return;

        int index = targets.IndexOf(currentTarget);

        if (index < 0)
        {
            SelectTarget(0);
            return;
        }

        SelectTarget(index + direction);
    }

    private void OnDestroy()
    {
        // 관전 중 카메라가 루트에 분리되어 있다면 정리
        if (spectating && playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
        }
    }
}
