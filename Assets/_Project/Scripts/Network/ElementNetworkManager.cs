using Mirror;
using UnityEngine;

public class ElementNetworkManager : NetworkManager
{
    private const int RequiredPlayerCount = 4;

    public void StartGame()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Only the host can start the game.");
            return;
        }

        if (NetworkServer.connections.Count != RequiredPlayerCount)
        {
            Debug.LogWarning(
                $"Cannot start game. Players: {NetworkServer.connections.Count}/{RequiredPlayerCount}"
            );
            return;
        }

        ServerChangeScene("GameScene");
    }
}