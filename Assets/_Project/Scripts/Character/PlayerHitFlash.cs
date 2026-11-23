
using System.Collections;
using UnityEngine;

public class PlayerHitFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] bodyRenderers;

    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private Coroutine flashCoroutine;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    public void PlayHitFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        SetColor(hitColor);

        yield return new WaitForSeconds(flashDuration);

        ClearColor();
        flashCoroutine = null;
    }

    private void SetColor(Color color)
    {
        foreach (Renderer r in bodyRenderers)
        {
            if (r == null) continue;

            r.GetPropertyBlock(propertyBlock);

            foreach (Material mat in r.sharedMaterials)
            {
                // URP Lit / Standard
                string colorProperty =
                    mat.HasProperty("_BaseColor") ?
                    "_BaseColor" : "_Color";

                propertyBlock.SetColor(colorProperty, color);
            }

            r.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearColor()
    {
        foreach (Renderer r in bodyRenderers)
        {
            if (r != null)
                r.SetPropertyBlock(null);
        }
    }
}
