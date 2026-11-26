
using UnityEngine;

public class GameEndAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private bool hasPlayed = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayEndBell()
    {
        if (hasPlayed || audioSource == null)
            return;

        hasPlayed = true;
        audioSource.Play();
    }

    public void ResetEndBell()
    {
        hasPlayed = false;
        audioSource.Stop();
    }
}

