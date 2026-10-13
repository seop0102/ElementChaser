using Mirror;
using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;

    private void Update()
    {
        if (healthText == null)
            return;

        if (!NetworkClient.active ||
            NetworkClient.localPlayer == null)
        {
            healthText.text = "";
            return;
        }

        PlayerHealth health =
            NetworkClient.localPlayer.GetComponent<PlayerHealth>();

        if (health == null)
        {
            healthText.text = "";
            return;
        }

        string hearts = "";

        for (int i = 0; i < health.MaxHealth; i++)
        {
            hearts += i < health.CurrentHealth
                ? "<color=#FF6666>♥</color>"
                : "<color=#777777>♡</color>";
        }

        healthText.text = hearts;
    }
}