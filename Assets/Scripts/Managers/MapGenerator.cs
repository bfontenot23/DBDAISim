using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 5;
    public int height = 5;

    [Header("Tile Prefabs")]
    public GameObject mazeTilePrefab;
    public GameObject fillerTilePrefab;
    public GameObject[] horizontalTiles;
    public GameObject[] verticalTiles;
    public GameObject[] cornerTiles;

    enum TileType 
    { 
        Border, 
        Maze, 
        Filler 
    }

    private TileType[,] mapLayout;
    private GameObject[,] placedTiles;
    void Start()
    {
        Debug.Log(GetTileSize(fillerTilePrefab));
        GenerateMap();
    }

    void GenerateMap()
    {
        placedTiles = new GameObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for ( int y = 0; y < height; y++)
            {
                GameObject tilePrefab;                
                //random rotation for tiles in 90 degree variations
                Quaternion rotation = Quaternion.identity;
               if (x == 0 && y==0) //bottom left corner
                {
                    tilePrefab = GetRandom(cornerTiles);
                    rotation = Quaternion.Euler(0, 0, 0);
                }
                else if ( x == 0 && y == height - 1) //top left corner
                {
                    tilePrefab = GetRandom(cornerTiles);
                    rotation = Quaternion.Euler(0, 0, 90);
                }
                else if (x == width -1  && y == height - 1) //top right corner
                {
                    tilePrefab = GetRandom(cornerTiles);
                    rotation = Quaternion.Euler(0, 0, 180);
                }
                else if (x == width - 1 && y == 0) //bottom right corner
                {
                    tilePrefab = GetRandom(cornerTiles);
                    rotation = Quaternion.Euler(0, 0, 270);
                }
                else if (y == 0 || y == height -1)
                {
                    tilePrefab = GetRandom(horizontalTiles);
                }
                else if (x == 0 || x == width - 1)
                {
                    tilePrefab = GetRandom(verticalTiles);
                }
                else
                {
                    float rand = Random.value;
                    if (rand < 0.7)
                        tilePrefab = fillerTilePrefab;
                    else
                        tilePrefab = mazeTilePrefab;
                }
                float tileSize = GetTileSize(fillerTilePrefab);
                Vector3 position = new Vector3(x * tileSize, y * tileSize, 0);
                placedTiles[x, y] = Instantiate(tilePrefab, position, rotation);
            }
        }
        CenterMap();
    }

    float GetTileSize(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return sr.bounds.size.x; // Assuming square tiles
        }
        return 16f; // Default size if no SpriteRenderer found
    }
    GameObject GetRandom(GameObject[] array)
    {
        if (array == null || array.Length == 0) 
        {
        return null;      
        }
        return array[Random.Range(0, array.Length)];
    }
    void CenterMap()
    {
        if (placedTiles == null) return;
        float size = GetTileSize(placedTiles[0, 0]);
        float offsetX = (width * size) / 2f - size / 2f;
        float offsetY = (height * size) / 2f - size / 2f;
        foreach (GameObject tile in placedTiles)
        {
            if (tile == null) continue;
            {
                tile.transform.position -= new Vector3(offsetX, offsetY, 0);
            }
        }
    }
}