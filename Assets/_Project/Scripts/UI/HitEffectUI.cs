
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitEffectUI : MonoBehaviour
{
    [SerializeField] private Image hitImage;
    [SerializeField] private float flashDuration = 0.3f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (hitImage == null)
            hitImage = GetComponent<Image>();

        SetAlpha(0f);
    }

    public void Flash()
    {
        if (hitImage == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetAlpha(1f);

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha = 1f - Mathf.Clamp01(elapsed / flashDuration);
            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);
        flashCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (hitImage == null)
            return;

        Color color = hitImage.color;
        color.a = alpha;
        hitImage.color = color;
    }
}
