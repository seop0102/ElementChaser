using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _settingPanel;
    [SerializeField] private Button _createRoomButton;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        _mainMenuPanel.SetActive(true);
        _lobbyPanel.SetActive(false);
        _settingPanel.SetActive(false);

        _createRoomButton.interactable = true;
    }

    public void ShowLobby()
    {
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
}