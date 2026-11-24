using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Player Slots")]
    [SerializeField] private Image[] playerBackgrounds;
    [SerializeField] private TMP_Text[] playerNames;

    [Header("Role Colors")]
    [SerializeField] private Color runnerColor =
        new Color32(195, 73, 73, 255);

    [SerializeField] private Color chaserColor =
        new Color32(50, 131, 77, 255);

    [SerializeField] private Color jailColor =
        new Color32(116, 119, 128, 255);

    [SerializeField] private Color waitingColor =
        new Color32(60, 60, 60, 255);

    private void Update()
    {
        if (playerBackgrounds == null ||
            playerNames == null)
            return;

        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            )
            .OrderBy(p => p.netId)
            .ToArray();

        int slotCount = Mathf.Min(
            playerBackgrounds.Length,
            playerNames.Length
        );

        for (int i = 0; i < slotCount; i++)
        {
            Image background = playerBackgrounds[i];
            TMP_Text nameText = playerNames[i];

            if (background == null || nameText == null)
                continue;

            if (GameStateManager.Instance == null ||
                i >= players.Length)
            {
                nameText.text = "Waiting...";
                background.color = waitingColor;
                continue;
            }

            NetworkPlayer player = players[i];

            PlayerRoleSync roleSync =
                player.GetComponent<PlayerRoleSync>();

            nameText.text =
                string.IsNullOrWhiteSpace(player.SteamName)
                    ? "Connecting..."
                    : player.SteamName;

            if (roleSync == null)
            {
                background.color = waitingColor;
                continue;
            }

            if (roleSync.IsJailed)
            {
                background.color = jailColor;
            }
            else if (roleSync.IsChaser)
            {
                background.color = chaserColor;
            }
            else
            {
                background.color = runnerColor;
            }
        }
    }
}