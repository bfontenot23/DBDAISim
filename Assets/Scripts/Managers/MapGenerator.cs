using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 5;
    public int height = 5;
 
    [Header("Filler Tiles")]
    public GameObject[] fillerTiles;

    [Header ("Generator Tiles")]
    public GameObject gymGenTile;
    public GameObject shackTile; 


    [Header("Border Tiles")]
    public GameObject[] horizontalTiles;
    public GameObject[] verticalTiles;
    public GameObject bottomLeftCorner;
    public GameObject bottomRightCorner;
    public GameObject topLeftCorner;
    public GameObject topRightCorner;

    private GameObject[,] placedTiles;
    private GameObject mapParent;
    private GameObject genFillerContainer;
    
    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        mapParent = new GameObject("Map_Generated");
        mapParent.transform.position = Vector3.zero;
        
        genFillerContainer = new GameObject("GenFiller");
        genFillerContainer.transform.SetParent(mapParent.transform);
        genFillerContainer.transform.position = Vector3.zero;
        
        placedTiles = new GameObject[width, height];
        float size = 16f; 
        int shackX = -1;
        int shackY = -1;
            if (width > 2 && height > 2)
            {
                shackX = Random.Range(1, width - 1);
                shackY = Random.Range(1, height - 1);
            }

        float offsetX = (width - 1) * size / 2f;
        float offsetY = (height - 1) * size / 2f;

        for (int x = 0; x < width; x++)
        {
            for ( int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * size - offsetX, y * size - offsetY, 0);
                GameObject tilePrefab = null;
                Quaternion rotation = Quaternion.identity;

               if (x == 0 && y==0) //bottom left corner
                {
                    tilePrefab = bottomLeftCorner;
                    rotation = Quaternion.identity;
                }
                else if ( x == 0 && y == height - 1) //top left corner
                {
                    tilePrefab = topLeftCorner;
                    rotation = Quaternion.identity;
                }
                else if (x == width -1  && y == height - 1) //top right corner
                {
                    tilePrefab = topRightCorner;
                    rotation = Quaternion.identity;
                }
                else if (x == width - 1 && y == 0) //bottom right corner
                {
                    tilePrefab = bottomRightCorner;
                    rotation = Quaternion.identity;
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
                    if (x == shackX && y == shackY)
                    {
                        tilePrefab = shackTile;
                    }
                    else  
                    {   
                        float rand = Random.value;
                        if (rand < 0.75f )
                        {
                            spawnFiller4(position);
                            continue;
                        }
                        else
                        {
                            tilePrefab = gymGenTile;
                        }
                    }
                }

                if (tilePrefab == null)
                {
                    Debug.LogError("No tile prefab assigned for position (" + x + ", " + y + ").");
                    continue;
                }

                GameObject instantiatedTile = Instantiate(tilePrefab, position, rotation, mapParent.transform);
                placedTiles[x, y] = instantiatedTile;
            }
        }
    }
    void spawnFiller4(Vector3 center)
    {
        float offset = 16f / 4f;
        Vector3[] positions = new Vector3[]
        {
            center + new Vector3(-offset, offset, 0),
            center + new Vector3(offset, offset, 0),
            center + new Vector3(-offset, -offset, 0),
            center + new Vector3(offset, -offset, 0)
        };
        foreach (Vector3 pos in positions)
        {
            GameObject prefab = GetRandom(fillerTiles);
            if (prefab == null)
            {
               Debug.LogError("Filler tiles array is empty or null.");
                continue;
            }
            Quaternion rotation = Quaternion.Euler(0, 0, 90 * Random.Range(0, 4));
            Instantiate(prefab, pos, rotation, genFillerContainer.transform);
        }
    }
    GameObject GetRandom(GameObject[] array)
    {
        if (array == null || array.Length == 0) 
        {
        return null;      
        }
        return array[Random.Range(0, array.Length)];
    }
}