using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using System.Collections;

public class MapVoteManager : NetworkBehaviour
{
    public static MapVoteManager Instance { get; private set; }

    private const int RequiredPlayers = 4;

    [SerializeField] private float voteDuration = 20f;

    [SyncVar] public int SquareVotes;
    [SyncVar] public int TriangleVotes;
    [SyncVar] public int CircleVotes;

    [SyncVar] public int TotalVotes;

    [SyncVar] public double VoteEndTime;
    [SyncVar] public bool VotingStarted;
    [SyncVar] public bool VotingFinished;

    // -1: 미결정, 0: 네모, 1: 세모, 2: 원
    [SyncVar] public int SelectedMap = -1;

    // 서버에서만 사용하는 투표 기록
    private readonly Dictionary<int, int> votes = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        votes.Clear();

        SquareVotes = 0;
        TriangleVotes = 0;
        CircleVotes = 0;
        TotalVotes = 0;

        SelectedMap = -1;
        VotingStarted = false;
        VotingFinished = false;
        VoteEndTime = 0;
    }

    [ServerCallback]
    private void Update()
    {
        if (VotingFinished)
            return;

        // 모든 플레이어의 씬 로딩이 끝날 때까지 대기
        if (!VotingStarted)
        {
            bool allReady =
                NetworkServer.connections.Count == RequiredPlayers &&
                NetworkServer.connections.Values.All(
                    conn => conn != null &&
                            conn.isReady &&
                            conn.identity != null
                );

            if (!allReady)
                return;

            VotingStarted = true;
            VoteEndTime = NetworkTime.time + voteDuration;

            Debug.Log("[MapVote] 투표 시작");
        }

        if (NetworkTime.time >= VoteEndTime)
        {
            FinishVoting();
        }
    }

    public void Vote(int mapIndex)
    {
        if (!isClient ||
            !VotingStarted ||
            VotingFinished)
            return;

        CmdVote(mapIndex);
    }

    [Command(requiresAuthority = false)]
    private void CmdVote(
        int mapIndex,
        NetworkConnectionToClient sender = null)
    {
        if (!VotingStarted ||
            VotingFinished ||
            NetworkTime.time >= VoteEndTime)
            return;

        if (sender == null ||
            sender.identity == null ||
            !sender.isReady)
            return;

        if (mapIndex < 0 || mapIndex > 2)
            return;

        int playerId = sender.connectionId;

        // 이미 투표한 플레이어는 중복 투표 불가
        if (votes.ContainsKey(playerId))
            return;

        votes.Add(playerId, mapIndex);

        UpdateVoteCounts();

        Debug.Log(
            $"[MapVote] Player {playerId} → Map {mapIndex}"
        );

        if (TotalVotes >= RequiredPlayers)
        {
            FinishVoting();
        }
    }

    [Server]
    private void UpdateVoteCounts()
    {
        SquareVotes = votes.Values.Count(v => v == 0);
        TriangleVotes = votes.Values.Count(v => v == 1);
        CircleVotes = votes.Values.Count(v => v == 2);

        TotalVotes = votes.Count;
    }

    [Server]
    private void FinishVoting()
    {
        if (VotingFinished)
            return;

        int[] counts =
        {
            SquareVotes,
            TriangleVotes,
            CircleVotes
        };

        int maxVotes = counts.Max();

        List<int> candidates = new();

        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] == maxVotes)
            {
                candidates.Add(i);
            }
        }

        // 동점인 맵 중 랜덤으로 결정
        SelectedMap = candidates[
            Random.Range(0, candidates.Count)
        ];

        VotingFinished = true;

        Debug.Log(
            $"[MapVote] 최종 선택 맵: {SelectedMap}"
        );
        StartCoroutine(MoveToGameScene());
    }
    [Server]
    private IEnumerator MoveToGameScene()
    {
        // 선택된 맵을 3초간 표시
        yield return new WaitForSecondsRealtime(3f);

        if (!NetworkServer.active)
            yield break;

        ElementNetworkManager manager =
            NetworkManager.singleton
                as ElementNetworkManager;

        if (manager == null)
        {
            Debug.LogError(
                "[MapVote] ElementNetworkManager 없음"
            );

            SteamLobbyManager.Instance?.LeaveLobby();

            yield break;
        }

        manager.StartSelectedMap(SelectedMap);
    }
    public static string GetMapName(int mapIndex)
    {
        switch (mapIndex)
        {
            case 0: return "SQUARE";
            case 1: return "TRIANGLE";
            case 2: return "CIRCLE";
            default: return "UNKNOWN";
        }
    }
}