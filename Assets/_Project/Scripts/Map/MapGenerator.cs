using UnityEngine;

public class MapGenerator : MonoBehaviour
{   
    public Tile normalTile;
    public Tile waterTile;
    public Tile fireTile;
    public Tile grassTile;
    public Tile windTile;

    public int width = 16;
    public int height = 16;

    public float tileSize = 1f; // 추후 타일 크기에 따라 변경 가능

    public bool normalOnly = false; // true이면 NormalTile만 생성, 테스트용

    private Tile[] tiles;

    void Start()
    {
        // 5개의 타일을 배열에 넣음
        tiles = new Tile[]
        {
            normalTile,
            waterTile,
            fireTile,
            grassTile,
            windTile
        };

        GenerateMap();
    }

    void GenerateMap()
    {
         for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Tile selectedTile;
                // 네 모서리는 무조건 NormalTile

            if (normalOnly)
            {
                selectedTile = normalTile;
            }
            else if ((x == 0 && z == 0) ||
                (x == 0 && z == height - 1) ||
                (x == width - 1 && z == 0) ||
                (x == width - 1 && z == height - 1))
            {
                selectedTile = normalTile;
            }
            else
            {
                // 0 ~ 4 중 랜덤 선택
                int randomIndex = Random.Range(0, tiles.Length);

                // 랜덤으로 선택된 타일
                selectedTile = tiles[randomIndex];
            }

                // 타일이 생성될 위치
                Vector3 position = new Vector3(
                    x * tileSize,
                    0,
                    z * tileSize
                );

                // 실제 타일 생성
                Instantiate(
                    selectedTile,
                    position,
                    Quaternion.identity
                );
            }
        }
    }
   
}
