using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerStatusText;

    private void Update()
    {
        if (playerStatusText == null)
            return;

        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            playerStatusText.text = "";
            return;
        }

        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                    FindObjectsSortMode.None
                )
                .OrderBy(p => p.netId)
                .ToArray();

        string result = "";

        foreach (NetworkPlayer player in players)
        {
            PlayerRoleSync roleSync =
                player.GetComponent<PlayerRoleSync>();

            if (roleSync == null)
                continue;

            string playerName =
                string.IsNullOrWhiteSpace(player.SteamName)
                    ? "Connecting..."
                    : player.SteamName;

            string status;

            if (roleSync.IsJailed)
            {
                status = "<color=#FFCC66>JAIL</color>";
            }
            else if (roleSync.IsChaser)
            {
                status = "<color=#FF6666>CHASER</color>";
            }
            else
            {
                status = "<color=#66FF99>RUNNER</color>";
            }

            result += $"{playerName}  -  {status}\n";
        }

        for (int i = players.Length; i < 4; i++)
        {
            result += "Waiting...  -  ---\n";
        }

        playerStatusText.text = result;
    }
}