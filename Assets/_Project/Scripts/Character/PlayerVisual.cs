using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Renderer characterRenderer;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        UpdateVisibility();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (characterRenderer == null)
            return;

        bool isGameScene =
            SceneManager.GetActiveScene().name == "GameScene";

        characterRenderer.enabled = isGameScene;
    }
}