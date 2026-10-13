
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElementNetworkManager : NetworkManager
{
    private const int RequiredPlayerCount = 4;
    
    private readonly HashSet<int> loadedPlayers = new HashSet<int>();
    
    private readonly HashSet<uint> spawnedPlayers = new HashSet<uint>();
    
    private bool spawnStarted = false;

    public void StartGame()
    {
        Debug.Log("[StartGame] 버튼으로 게임 시작 요청됨!");
        Debug.Log(
                $"[StartGame] Server={NetworkServer.active}, " +
                $"Client={NetworkClient.active}, " +
                $"Connections={NetworkServer.connections.Count}"
                );
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
        
        loadedPlayers.Clear();
        spawnedPlayers.Clear();
        spawnStarted = false;

        ServerChangeScene("GameScene");
    }
    
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName != "GameScene")
            return;

        Debug.Log("[Loading] 서버 GameScene 로딩 완료");

        TryStartSpawn();
    }
    
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        loadedPlayers.Add(conn.connectionId);

        Debug.Log(
            $"[Loading] {loadedPlayers.Count}/{RequiredPlayerCount}명 Ready"
        );

        TryStartSpawn();
    }
    
    [Server]
    private void TryStartSpawn()
    {
        if (spawnStarted)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        if (loadedPlayers.Count != RequiredPlayerCount)
            return;

        spawnStarted = true;

        StartCoroutine(AssignSpawnPositions());
    }
    
    [Server]
    private IEnumerator AssignSpawnPositions()
    {
        PlayerMovement[] players = null;

        float timeout = Time.time + 10f;
        
        while (Time.time < timeout)
        {
            players = FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

            if (players.Length == RequiredPlayerCount &&
                players.All(p => p.isServer))
            {
                break;
            }

            yield return null;
        }

        if (players == null ||
            players.Length != RequiredPlayerCount ||
            players.Any(p => !p.isServer))
        {
            Debug.LogError("[Spawn] 플레이어 준비 실패");
            yield break;
        }
        
        players = players.OrderBy(p => p.netId).ToArray();
        
        Transform[] spawnPoints = new Transform[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            string spawnName = $"SpawnPoint{i + 1:00}";

            GameObject spawnPoint = GameObject.Find(spawnName);

            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"[Spawn] {spawnName}을 찾을 수 없습니다."
                );
                yield break;
            }

            spawnPoints[i] = spawnPoint.transform;
        }
        
        spawnedPlayers.Clear();

        for (int i = 0; i < players.Length; i++)
        {
            Vector3 position =
                spawnPoints[i].position + Vector3.up * 2f;

            players[i].SetSpawnPosition(
                position,
                spawnPoints[i].rotation
            );
        }

        Debug.Log("[Spawn] 모든 플레이어 스폰 위치 배정 완료");
        Debug.Log("[Spawn] 클라이언트 스폰 완료 응답 대기");
    }
    
    [Server]
    public void ConfirmPlayerSpawn(uint playerNetId)
    {
        if (!spawnStarted)
            return;

        if (!NetworkServer.spawned.TryGetValue(
            playerNetId,
            out NetworkIdentity playerIdentity))
        {
            return;
        }
        
        if (playerIdentity.connectionToClient == null)
            return;
        
        if (!spawnedPlayers.Add(playerNetId))
            return;

        Debug.Log(
            $"[Spawn] {spawnedPlayers.Count}/{RequiredPlayerCount}명 스폰 완료"
        );

        if (spawnedPlayers.Count == RequiredPlayerCount)
        {
            StartGameAfterSpawn();
        }
    }
    
    [Server]
    private void StartGameAfterSpawn()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            Debug.LogError(
                "[GameState] GameStateManager를 찾을 수 없습니다."
            );
            return;
        }

        Debug.Log("[Spawn] 모든 플레이어 준비 완료");

        gameState.StartGameSequence();
    }
    
    
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        // 호스트 종료로 연결이 끊긴 클라이언트도
        // Steam 로비에서 정상 퇴장
        if (!NetworkServer.active &&
            SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.HandleDisconnected();
        }
    }

}
