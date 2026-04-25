using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Multi-Map Configuration")]
    public int numberOfMaps = 1;
    public float mapSpacing = 2f;
    
    [Header("Map Size")]
    public int width = 5;
    public int height = 5;
 
    [Header("Filler Tiles")]
    public GameObject[] fillerTiles;

    [Header ("Generator Tiles")]
    public GameObject gymGenTile;
    public GameObject shackTile;
    public GameObject mainBuildingTile;

    [Header("Border Tiles")]
    public GameObject[] horizontalTiles;
    public GameObject[] verticalTiles;
    public GameObject bottomLeftCorner;
    public GameObject bottomRightCorner;
    public GameObject topLeftCorner;
    public GameObject topRightCorner;
    
    [Header("Pallet Generation")]
    public GameObject palletPrefab;
    [Range(10, 20)]
    public int minPallets = 10;
    [Range(10, 20)]
    public int maxPallets = 20;

    [Header("Generator Generation")]
    public GameObject generatorPrefab;

    private GameObject[,] placedTiles;
    private GameObject mapParent;
    private GameObject genFillerContainer;
    private int gymTileCount = 0;
    private int maxGymTiles;
    private GeneratePallets palletGenerator;
    private GameObject multiMapContainer;
    private GenerateGenerators generatorSpawner;
    
    void Start()
    {
        if (numberOfMaps <= 1)
        {
            GenerateMap(Vector3.zero);
            SetupPalletGenerator();
            SetupGeneratorSpawner();
            // Delay pallet generation to ensure all tiles have completed their Awake() and generation
            if (palletGenerator != null)
            {
                Invoke(nameof(TriggerPalletGeneration), 0.5f);
            }
        }
        else
        {
            GenerateMultipleMaps();
        }
    }
    
    void GenerateMultipleMaps()
    {
        multiMapContainer = new GameObject("MultiMap_Container");
        multiMapContainer.transform.position = Vector3.zero;
        
        // Calculate grid dimensions
        int[] gridDims = CalculateGridDimensions(numberOfMaps);
        int gridWidth = gridDims[0];
        int gridHeight = gridDims[1];
        
        float mapWidth = width * 16f;
        float mapHeight = height * 16f;
        float spacingX = mapSpacing * 16f;
        float spacingY = mapSpacing * 16f;
        
        int mapsGenerated = 0;
        
        // Generate maps in grid pattern, Map_0 centered at (0,0), expanding right and downward
        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                if (mapsGenerated >= numberOfMaps)
                    break;
                    
                if (ShouldSkipPosition(col, row, gridWidth, gridHeight, numberOfMaps))
                    continue;
                
                float xPos = col * (mapWidth + spacingX);
                float yPos = -row * (mapHeight + spacingY);
                Vector3 mapPosition = new Vector3(xPos, yPos, 0);
                
                GenerateMap(mapPosition, mapsGenerated);
                mapsGenerated++;
            }
        }
        if (generatorSpawner != null)
        {
            Invoke(nameof(TriggerGeneratorGeneration), 0.5f);
        }
    }
    
    int[] CalculateGridDimensions(int numMaps)
    {
        if (numMaps <= 1)
            return new int[] { 1, 1 };
        
        // Calculate grid dimensions to prefer square-ish layouts
        int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(numMaps));
        int gridHeight = Mathf.CeilToInt((float)numMaps / gridWidth);
        
        return new int[] { gridWidth, gridHeight };
    }
    
    bool ShouldSkipPosition(int col, int row, int gridWidth, int gridHeight, int totalMaps)
    {
        int totalGridPositions = gridWidth * gridHeight;
        if (totalGridPositions <= totalMaps)
            return false;
        
        // Skip positions at the end (bottom-right corner)
        int positionIndex = row * gridWidth + col;
        return positionIndex >= totalMaps;
    }
    
    void SetupPalletGenerator(GameObject mapRoot = null)
    {
        GameObject targetMap = mapRoot != null ? mapRoot : mapParent;
        
        if (targetMap == null)
            return;
            
        // Add GeneratePallets component to the map root
        GeneratePallets generator = targetMap.AddComponent<GeneratePallets>();
        generator.palletPrefab = palletPrefab;
        generator.minPallets = minPallets;
        generator.maxPallets = maxPallets;
        
        if (numberOfMaps <= 1)
        {
            palletGenerator = generator;
        }
        else
        {
            // For multiple maps, trigger immediately for each map
            Invoke(nameof(TriggerPalletGenerationForMap), 0.5f);
        }
    }
    
    void TriggerPalletGenerationForMap()
    {
        if (multiMapContainer != null)
        {
            GeneratePallets[] generators = multiMapContainer.GetComponentsInChildren<GeneratePallets>();
            foreach (var gen in generators)
            {
                gen.OnMapGenerationComplete();
            }
        }
    }
    
    void TriggerPalletGeneration()
    {
        if (palletGenerator != null)
        {
            palletGenerator.OnMapGenerationComplete();
        }
    }

    void SetupGeneratorSpawner()
    {
        if (mapParent == null)
            return;
        generatorSpawner = mapParent.AddComponent<GenerateGenerators>();
        generatorSpawner.generatorPrefab = generatorPrefab; 
    }
    void TriggerGeneratorGeneration()
    {
        if (generatorSpawner != null)
        {
            generatorSpawner.OnMapGenerationComplete();
        }
    }

    void GenerateMap(Vector3 mapOffset, int mapIndex = 0)
    {
        string mapName = numberOfMaps > 1 ? $"Map_{mapIndex}" : "Map_Generated";
        mapParent = new GameObject(mapName);
        mapParent.transform.position = Vector3.zero;
        
        if (multiMapContainer != null)
        {
            mapParent.transform.SetParent(multiMapContainer.transform);
        }
        
        genFillerContainer = new GameObject("GenFiller");
        genFillerContainer.transform.SetParent(mapParent.transform);
        genFillerContainer.transform.localPosition = Vector3.zero;
        
        gymTileCount = 0;
        float[] weights = { 0.5f, 0.3f, 0.2f };
        float rand = Random.value;
        if (rand < weights[0])
            maxGymTiles = 3;
        else if (rand < weights[0] + weights[1])
            maxGymTiles = 4;
        else
            maxGymTiles = 5;
        
        placedTiles = new GameObject[width, height];
        float size = 16f; 
        int shackX = -1;
        int shackY = -1;
        int mainBuildingX = -1;
        int mainBuildingY = -1;
        
        if (width > 2 && height > 2)
        {
            shackX = Random.Range(1, width - 1);
            shackY = Random.Range(1, Mathf.Min(3, height - 1));
        }
        
        if (width > 3 && height > 3)
        {
            int centerX = width / 2;
            mainBuildingX = Random.Range(Mathf.Max(1, centerX - 1), Mathf.Min(width - 3, centerX + 1));
            mainBuildingY = Random.Range(Mathf.Max(height - 3, 1), height - 2);
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
                    if (x == mainBuildingX && y == mainBuildingY)
                    {
                        spawnMainBuilding(position);
                        continue;
                    }
                    else if ((x == mainBuildingX + 1 && y == mainBuildingY) ||
                             (x == mainBuildingX && y == mainBuildingY + 1) ||
                             (x == mainBuildingX + 1 && y == mainBuildingY + 1))
                    {
                        continue;
                    }
                    else if (x == shackX && y == shackY)
                    {
                        tilePrefab = shackTile;
                    }
                    else  
                    {   
                        float tileRand = Random.value;
                        if (tileRand < 0.75f || gymTileCount >= maxGymTiles)
                        {
                            spawnFiller4(position);
                            continue;
                        }
                        else
                        {
                            tilePrefab = gymGenTile;
                            gymTileCount++;
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
        
        // Move the parent to the correct grid position after all tiles are placed as children
        mapParent.transform.position = mapOffset;
        
        // Setup pallet generator for this map if in multi-map mode
        if (numberOfMaps > 1)
        {
            SetupPalletGenerator(mapParent);
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
    
    void spawnMainBuilding(Vector3 bottomLeft)
    {
        if (mainBuildingTile == null)
        {
            Debug.LogError("Main building tile is not assigned!");
            return;
        }
        
        float size = 16f;
        Vector3 centerPosition = bottomLeft + new Vector3(size / 2f, size / 2f, 0);
        Instantiate(mainBuildingTile, centerPosition, Quaternion.identity, mapParent.transform);
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