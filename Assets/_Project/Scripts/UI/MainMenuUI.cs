using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _settingPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private GameObject _hostDisconnectedPanel;

    private Coroutine _hostNoticeCoroutine;
    private void Start()
    {
        if (SteamLobbyManager.Instance != null &&
            SteamLobbyManager.Instance.ShouldShowLobby)
        {
            ShowLobby();
        }
        else
        {
            ShowMainMenu();
        }
    }
    
    public void OnClickCreateRoom()
    {
        if (SteamLobbyManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] SteamLobbyManager 없음");
            return;
        }

        SteamLobbyManager.Instance.CreateLobby();
    }

    public void ShowMainMenu()
    {
        //메인화면에서 커서 잠기는 문제 해결 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _mainMenuPanel.SetActive(true);
        _lobbyPanel.SetActive(false);
        _settingPanel.SetActive(false);

        _createRoomButton.interactable = true;
    }

    public void ShowLobby()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _mainMenuPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
        _settingPanel.SetActive(false);
    }

    public void ShowSetting()
    {
        _mainMenuPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _settingPanel.SetActive(true);
    }

    public void SetCreateRoomInteractable(bool interactable)
    {
        _createRoomButton.interactable = interactable;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void ShowHostDisconnectedNotice()
    {
        ShowMainMenu();

        if (_hostDisconnectedPanel == null)
        {
            Debug.LogError("[MainMenuUI] 안내 패널 연결 안 됨");
            return;
        }

        if (_hostNoticeCoroutine != null)
        {
            StopCoroutine(_hostNoticeCoroutine);
        }

        _hostNoticeCoroutine =
            StartCoroutine(HostDisconnectedNoticeRoutine());
    }

    private IEnumerator HostDisconnectedNoticeRoutine()
    {
        _hostDisconnectedPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(3f);

        _hostDisconnectedPanel.SetActive(false);
        _hostNoticeCoroutine = null;
    }
}