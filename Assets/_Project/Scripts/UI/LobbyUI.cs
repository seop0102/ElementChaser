using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] _playerNameTexts;
    [SerializeField] private GameObject[] _hostTexts;

    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private Button _startGameButton;

    public void OnClickStartGame()
    {
        Debug.Log("[LobbyUI] START 버튼 클릭!");

        ElementNetworkManager manager =
            NetworkManager.singleton as ElementNetworkManager;

        if (manager == null)
        {
            Debug.LogError("[LobbyUI] ElementNetworkManager 없음!");
            return;
        }

        manager.StartGame();
    }
    public void RefreshLobby()
    {
        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );
        NetworkPlayer[] sortedPlayers = players
            .OrderBy(player => player.netId)
            .ToArray();

        for (int i = 0; i < _playerNameTexts.Length; i++)
        {
            if (i < sortedPlayers.Length)
            {
                NetworkPlayer player = sortedPlayers[i];

                _playerNameTexts[i].text =
                    string.IsNullOrWhiteSpace(player.SteamName)
                        ? "-"
                        : player.SteamName;

                _hostTexts[i].SetActive(player.IsLobbyHost);
            }
            else
            {
                _playerNameTexts[i].text = "";
                _hostTexts[i].SetActive(false);
            }
        }

        ElementNetworkManager manager =
            NetworkManager.singleton as ElementNetworkManager;

        int playerCount = manager != null
            ? manager.LobbyPlayerCount
            : 0;

        _playerCountText.text = $"{playerCount} / 4";

        bool isHost = NetworkServer.active;

        _startGameButton.gameObject.SetActive(isHost);
        bool namesReady =
            sortedPlayers.Length == 4 &&
            sortedPlayers.All(p =>
                !string.IsNullOrWhiteSpace(p.SteamName));

        _startGameButton.interactable =
            isHost &&
            playerCount == 4 &&
            namesReady;
        
        CanvasGroup canvasGroup =
            _startGameButton.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                _startGameButton.interactable ? 1f : 0.5f;
        }
        LobbyCharacterPreviewManager previewManager =
            FindAnyObjectByType<LobbyCharacterPreviewManager>();

        if (previewManager != null)
        {
            previewManager.RefreshPreviews();
        }

    }
    public void RefreshAfterPlayerRemoved()
    {
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(RefreshNextFrame());
    }

    private System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;

        RefreshLobby();
    }
    private void OnEnable()
    {
        RefreshLobby();
    }
    private void Awake()
    {
        if (_startGameButton == null)
        {
            Debug.LogError("[LobbyUI] StartGameButton 연결 안 됨!");
            return;
        }

        // 기존 Inspector 이벤트 제거 후 현재 LobbyUI에 연결
        _startGameButton.onClick.RemoveAllListeners();
        _startGameButton.onClick.AddListener(OnClickStartGame);

        Debug.Log("[LobbyUI] START 버튼 연결 완료");
    }
    
    public void OnClickBack()
    {
        if (SteamLobbyManager.Instance != null)
            SteamLobbyManager.Instance.LeaveLobby();
    }

}