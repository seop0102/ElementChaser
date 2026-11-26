
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitEffectUI : MonoBehaviour
{
    [Header("UI 이미지")]
    [SerializeField] private Image hitImage;
    [SerializeField] private Sprite angelSprite;
    [SerializeField] private Sprite devilSprite;

    [Header("피격 효과")]
    [SerializeField] private float flashDuration = 0.3f;

    private Coroutine flashCoroutine;
    private bool isChaser = false;
    private bool isDeathEffectActive = false;


    private void Awake()
    {
        if (hitImage == null)
            hitImage = GetComponent<Image>();

        SetAlpha(0f);
    }

    public void SetRoleUI(bool chaser)
    {
        isChaser = chaser;

        if (hitImage == null)
            return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        hitImage.sprite = isChaser
            ? devilSprite
            : angelSprite;

        // 술래는 상시 표시, 천사는 피격 시에만 표시
        SetAlpha(isChaser ? 1f : 0f);
    }

    public void Flash()
    {
        // 술래는 상시 표시이므로 피격 효과 생략
        if (isChaser || isDeathEffectActive || hitImage == null)
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

            float alpha = 1f -
                Mathf.Clamp01(elapsed / flashDuration);

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
       // 사망 시 천사 구름 UI를 계속 표시
public void ShowDeathEffect()
{
    isDeathEffectActive = true;
    if (hitImage == null || isChaser)
        return;

    if (flashCoroutine != null)
    {
        StopCoroutine(flashCoroutine);
        flashCoroutine = null;
    }

    hitImage.sprite = angelSprite;
    SetAlpha(1f);
}

// 사망 효과 초기화
public void HideDeathEffect()
{
    isDeathEffectActive = false;
    if (hitImage == null)
        return;

    SetAlpha(isChaser ? 1f : 0f);
}
}
