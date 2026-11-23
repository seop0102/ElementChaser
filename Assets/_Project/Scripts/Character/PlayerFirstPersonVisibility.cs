
using Mirror;
using UnityEngine;

public class PlayerFirstPersonVisibility : NetworkBehaviour
{
    [Header("내 화면에서 숨길 렌더러")]
    [SerializeField] private Renderer[] hideRenderers;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SetBodyVisible(false);
    }

    public override void OnStopLocalPlayer()
    {
        SetBodyVisible(true);
        base.OnStopLocalPlayer();
    }

    private void SetBodyVisible(bool visible)
    {
        foreach (Renderer renderer in hideRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}
