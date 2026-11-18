using UnityEngine;

public class GameReturnNotice : MonoBehaviour
{
    [SerializeField]
    private GameObject _noticePanel;

    private void Awake()
    {
        if (_noticePanel != null)
            _noticePanel.SetActive(false);
    }

    public void ShowNotice()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_noticePanel != null)
            _noticePanel.SetActive(true);

        Debug.Log("[UI] 플레이어 이탈 안내 표시");
    }
}