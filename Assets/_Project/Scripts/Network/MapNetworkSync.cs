using Mirror;
using UnityEngine;

public class MapNetworkSync : NetworkBehaviour
{
    public static MapNetworkSync Instance { get; private set; }

    [Header("Map Generator")]
    [SerializeField] private MapGenerator mapGenerator;

    [SyncVar] private int selectedMapIndex = -1;
    [SyncVar] private int selectedMapSeed;

    public int SelectedMapIndex => selectedMapIndex;
    public int SelectedMapSeed => selectedMapSeed;

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

        ElementNetworkManager manager =
            NetworkManager.singleton
                as ElementNetworkManager;

        if (manager == null)
        {
            Debug.LogError("[MapSync] NetworkManager 없음");
            return;
        }

        selectedMapIndex = manager.SelectedMapIndex;
        selectedMapSeed = manager.SelectedMapSeed;

        Debug.Log(
            $"[MapSync] 서버 맵 정보 설정 | " +
            $"Map={selectedMapIndex}, Seed={selectedMapSeed}"
        );
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (selectedMapIndex < 0 ||
            selectedMapIndex > 2)
        {
            Debug.LogError("[MapSync] 잘못된 맵 정보");
            return;
        }

        if (mapGenerator == null)
        {
            Debug.LogError("[MapSync] MapGenerator 연결 누락");
            return;
        }

        MapGenerator.MapShape selectedShape =
            (MapGenerator.MapShape)(selectedMapIndex);

        // 실제 맵 생성
        mapGenerator.GenerateMap(selectedShape, selectedMapSeed);

        Debug.Log(
            $"[MapSync] 맵 정보 수신 | " +
            $"Shape={selectedShape}, Seed={selectedMapSeed}"
        );
        
    }
}