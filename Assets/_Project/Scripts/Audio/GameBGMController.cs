
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameBGMController : MonoBehaviour
{
    private AudioSource bgm;

    private void Awake()
    {
        bgm = GetComponent<AudioSource>();
        bgm.playOnAwake = false;
        bgm.loop = true;
    }

    private void Update()
    {
        if (GameStateManager.Instance == null)
        {
            if (bgm.isPlaying)
                bgm.Stop();

            return;
        }

        bool isPlaying =
            GameStateManager.Instance.CurrentPhase ==
            GamePhase.Playing;

        if (isPlaying && !bgm.isPlaying)
        {
            bgm.Play();
        }
        else if (!isPlaying && bgm.isPlaying)
        {
            bgm.Stop();
        }
    }
}
