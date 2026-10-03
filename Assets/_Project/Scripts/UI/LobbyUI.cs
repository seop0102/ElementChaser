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
    [SerializeField] private TMP_Text _backButtonText;

    public void RefreshLobby()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>();

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
                        ? "Connecting..."
                        : player.SteamName;

                _hostTexts[i].SetActive(player.IsLobbyHost);
            }
            else
            {
                _playerNameTexts[i].text = "Waiting...";
                _hostTexts[i].SetActive(false);
            }
        }

        int playerCount = sortedPlayers.Length;

        _playerCountText.text = playerCount == 4
            ? "Ready! 4 / 4"
            : $"Waiting for players... {playerCount} / 4";

        bool isHost = NetworkServer.active;

        _startGameButton.gameObject.SetActive(isHost);
        _startGameButton.interactable = isHost && playerCount == 4;

        _backButtonText.text = isHost ? "Close Room" : "Leave Room";
    }
}