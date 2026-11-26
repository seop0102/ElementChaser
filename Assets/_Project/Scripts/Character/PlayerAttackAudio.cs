
using Mirror;
using UnityEngine;

public class PlayerAttackAudio : NetworkBehaviour
{
    [Header("공격 사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackClip;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // 서버에서 공격자와 대상에게만 재생 요청
    [Server]
    public void ServerPlayAttackSound(
        NetworkIdentity targetIdentity)
    {
        if (connectionToClient != null)
            TargetPlayAttackSound(connectionToClient);

        if (targetIdentity == null)
            return;

        NetworkConnectionToClient targetConnection =
            targetIdentity.connectionToClient;

        if (targetConnection != null &&
            targetConnection != connectionToClient)
        {
            TargetPlayAttackSound(targetConnection);
        }
    }

    [TargetRpc]
    private void TargetPlayAttackSound(
        NetworkConnectionToClient target)
    {
        if (audioSource == null || attackClip == null)
            return;

        audioSource.PlayOneShot(attackClip);
    }
}
