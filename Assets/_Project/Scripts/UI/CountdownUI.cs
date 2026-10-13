using Mirror;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text chaserNameText;
    [SerializeField] private TMP_Text countdownText;

    private void Update()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            countdownPanel.SetActive(false);
            return;
        }

        bool isCountdown =
            gameState.CurrentPhase == GamePhase.Countdown;

        countdownPanel.SetActive(isCountdown);

        if (!isCountdown)
            return;

        countdownText.text = gameState.Countdown.ToString();

        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayer player in players)
        {
            if (player.netId == gameState.ChaserNetId)
            {
                chaserNameText.text =
                    string.IsNullOrWhiteSpace(player.SteamName)
                        ? "Connecting..."
                        : player.SteamName;

                return;
            }
        }

        chaserNameText.text = "Connecting...";
    }
}