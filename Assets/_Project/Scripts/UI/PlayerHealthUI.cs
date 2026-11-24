using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Heart Images")]
    [SerializeField] private Image[] heartImages;

    [Header("Heart Sprites")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private void Update()
    {
        PlayerHealth health = null;

        if (NetworkClient.active &&
            NetworkClient.localPlayer != null)
        {
            health = NetworkClient.localPlayer
                .GetComponent<PlayerHealth>();
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            Image heart = heartImages[i];

            if (heart == null)
                continue;

            bool visible =
                health != null && i < health.MaxHealth;

            heart.gameObject.SetActive(visible);

            if (!visible)
                continue;

            heart.sprite = i < health.CurrentHealth
                ? fullHeart
                : emptyHeart;
        }
    }
}