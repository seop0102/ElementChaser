
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour
{
    public enum MapShape
    {
        Square,
        Triangle,
        Circle
    }

    [Header("Map Settings")]
    public MapShape mapShape;
    public int width = 10;
    public int height = 10;

    public int triangleRows = 10;
    public float circleRadius = 5.5f;

    public float tileSize = 10f;

    [Header("Tile Prefabs")]
    public Tile waterTile;
    public Tile fireTile;
    public Tile grassTile;
    public Tile windTile;

    private Tile[] tiles;
    private System.Random rng;

    void Awake()
    {
        // 4개의 타일을 배열에 넣음
        tiles = new Tile[]
        {
            waterTile,
            fireTile,
            grassTile,
            windTile
        };
    }

    // 테스트용
    void Start()
    {
        // 실제 네트워크 연결 시에는 비활성화
        //GenerateMap(mapShape, 1);
    }

    public void GenerateMap(MapShape selectedShape, int seed)
    {
        mapShape = selectedShape;

        // Seed를 이용해 랜덤 생성기 초기화
        rng = new System.Random(seed);

        switch (selectedShape)
        {
            case MapShape.Square:
                GenerateSquareMap();
                break;

            case MapShape.Triangle:
                GenerateTriangleMap();
                break;

            case MapShape.Circle:
                GenerateCircleMap();
                break;
        }

        SetSpawnPoints(selectedShape);

        Debug.Log(
            $"[MapGenerator] {selectedShape} 맵 생성 완료 / Seed: {seed}"
        );
    }

    void GenerateSquareMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Tile selectedTile = GetRandomTile();

                Vector3 position = new Vector3(
                    x * tileSize,
                    0,
                    z * tileSize
                );

                Instantiate(
                    selectedTile,
                    position,
                    Quaternion.identity
                );
            }
        }
    }

    // 삼각형 맵 생성
    void GenerateTriangleMap()
    {
        for (int row = 0; row < triangleRows; row++)
        {
            int tileCount = row * 2 + 1;

            for (int x = 0; x < tileCount; x++)
            {
                Tile selectedTile = GetRandomTile();

                float xPosition = x - row;

                Vector3 position = new Vector3(
                    xPosition * tileSize,
                    0,
                    (triangleRows - 1 - row) * tileSize
                );

                Instantiate(
                    selectedTile,
                    position,
                    Quaternion.identity
                );
            }
        }
    }

    // 원형 맵 생성
    void GenerateCircleMap()
    {
        int radius = Mathf.CeilToInt(circleRadius);

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (x * x + z * z > circleRadius * circleRadius)
                    continue;

                Tile selectedTile = GetRandomTile();

                Vector3 position = new Vector3(
                    x * tileSize,
                    0,
                    z * tileSize
                );

                Instantiate(
                    selectedTile,
                    position,
                    Quaternion.identity
                );
            }
        }
    }

    // Seed 기반 랜덤 타일 선택
    Tile GetRandomTile()
    {
        int randomIndex = rng.Next(tiles.Length);

        return tiles[randomIndex];
    }

    void SetSpawnPoints(MapShape selectedShape)
    {
        GameObject spawnParent = GameObject.Find("SpawnPoints");

        if (spawnParent == null)
        {
            Debug.LogError("SpawnPoints 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Transform[] spawnPoints = new Transform[4];

        for (int i = 0; i < 4; i++)
        {
            spawnPoints[i] = spawnParent.transform.Find(
                "SpawnPoint0" + (i + 1)
            );

            if (spawnPoints[i] == null)
            {
                Debug.LogError(
                    "SpawnPoint0" + (i + 1) + "을 찾을 수 없습니다."
                );
                return;
            }
        }

        switch (selectedShape)
        {
            case MapShape.Square:
                SetSquareSpawnPoints(spawnPoints);
                break;

            case MapShape.Triangle:
                SetTriangleSpawnPoints(spawnPoints);
                break;

            case MapShape.Circle:
                SetCircleSpawnPoints(spawnPoints);
                break;
        }
    }

    // 정사각형: 네 모서리
    void SetSquareSpawnPoints(Transform[] points)
    {
        SetPosition(points[0], 0, 0);
        SetPosition(points[1], (width - 1) * tileSize, 0);
        SetPosition(points[2], 0, (height - 1) * tileSize);
        SetPosition(points[3], (width - 1) * tileSize, (height - 1) * tileSize);
    }

    // 삼각형: 세 꼭짓점 + 중심
    void SetTriangleSpawnPoints(Transform[] points)
    {
        float size = tileSize;
        float extent = triangleRows - 1;

        SetPosition(points[0], 0, extent * size);
        SetPosition(points[1], -extent * size, 0);
        SetPosition(points[2], extent * size, 0);
        SetPosition(points[3], 0, (extent / 3f) * size);
    }

    // 원형: 네 방향
    void SetCircleSpawnPoints(Transform[] points)
    {
        float radius = 5f * tileSize;

        SetPosition(points[0], 0, radius);
        SetPosition(points[1], radius, 0);
        SetPosition(points[2], 0, -radius);
        SetPosition(points[3], -radius, 0);
    }

    // 실제 위치 변경 (기존 Y 높이 유지)
    void SetPosition(Transform point, float x, float z)
    {
        Vector3 position = point.position;

        position.x = x;
        position.z = z;

        point.position = position;
    }
}
