
using UnityEngine;
using Mirror;

public class PlayerHammer : NetworkBehaviour
{
    private GameObject hammer;
    private PlayerRoleSync playerRole;

    private void Awake()
    {
        playerRole = GetComponent<PlayerRoleSync>();

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "hammer_7")
            {
                hammer = child.gameObject;
                break;
            }
        }

        if (hammer == null)
        {
            Debug.LogError("망치 오브젝트를 찾지 못했습니다!");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHammer();
    }

    private void Update()
    {
        if (playerRole == null || hammer == null)
            return;

        bool shouldShow = playerRole.IsChaser;

        if (hammer.activeSelf != shouldShow)
            hammer.SetActive(shouldShow);
    }

    private void UpdateHammer()
    {
        if (playerRole != null && hammer != null)
        {
            hammer.SetActive(playerRole.IsChaser);
        }
    }
}
