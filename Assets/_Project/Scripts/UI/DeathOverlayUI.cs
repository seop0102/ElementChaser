
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathOverlayUI : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    [Header("사망 화면 효과")]
    [SerializeField, Range(0f, 1f)]
    private float targetAlpha = 0.35f;

    [SerializeField]
    private float fadeDuration = 0.6f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (overlayImage == null)
            overlayImage = GetComponent<Image>();

        SetAlpha(0f);
    }

    public void ShowDeathOverlay()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startAlpha = overlayImage.color.a;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(fadeDuration, 0.001f)
            );

            SetAlpha(Mathf.Lerp(
                startAlpha, targetAlpha, t
            ));

            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    public void HideDeathOverlay()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (overlayImage == null)
            return;

        Color color = overlayImage.color;
        color.a = alpha;
        overlayImage.color = color;
    }
}
