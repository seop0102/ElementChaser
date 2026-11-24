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

    private void Awake()
    {
        if (_hostDisconnectedPanel != null)
            _hostDisconnectedPanel.SetActive(false);
    }

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

        Debug.Log("[MainMenuUI] UI 초기화 완료");
    }

    
    
    public void OnClickCreateRoom()
    {
        SteamLobbyManager lobbyManager = SteamLobbyManager.Instance;

        if (lobbyManager == null)
        {
            Debug.LogError("[MainMenuUI] SteamLobbyManager 없음");
            return;
        }

        if (lobbyManager.CurrentState != LobbyConnectionState.Idle)
        {
            Debug.LogWarning(
                $"[MainMenuUI] 방 생성 불가: {lobbyManager.CurrentState}"
            );
            return;
        }

        if (lobbyManager.IsMirrorRunning())
        {
            Debug.LogWarning("[MainMenuUI] 네트워크가 아직 활성화됨");
            return;
        }

        lobbyManager.CreateLobby();
    }


    
    public void ShowMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 메인 화면만 표시
        _mainMenuPanel.SetActive(true);
        _lobbyPanel.SetActive(false);
        _settingPanel.SetActive(false);

        // 실제 네트워크 상태에 따라 방 생성 버튼 활성화
        bool canCreateRoom =
            SteamLobbyManager.Instance != null &&
            SteamLobbyManager.Instance.CurrentState ==
            LobbyConnectionState.Idle &&
            !SteamLobbyManager.Instance.IsMirrorRunning();

        _createRoomButton.interactable = canCreateRoom;

        Debug.Log(
            $"[MainMenuUI] 메인 메뉴 표시, CreateRoom={canCreateRoom}"
        );
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