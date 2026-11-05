using UnityEngine;
public class GameReturnNotice : MonoBehaviour
{
    [SerializeField]
    private GameObject _noticePanel;

    private void Awake()
    {
        _noticePanel.SetActive(false);
    }

    public void ShowNotice()
    {
        _noticePanel.SetActive(true);

        Debug.Log("[UI] 플레이어 이탈 안내 표시");
    }
}