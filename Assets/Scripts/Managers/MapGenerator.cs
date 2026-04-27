using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Multi-Map Configuration")]
    public int numberOfMaps = 1;
    public float mapSpacing = 2f;
    
    [Header("Episode Settings")]
    public float episodeTimeLimitSeconds = 600f;
    
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
                Invoke(nameof(TriggerPalletGeneration), 0.5f);
            if (generatorSpawner != null)
                Invoke(nameof(TriggerGeneratorGeneration), 0.5f);
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
        Invoke(nameof(TriggerPalletGenerationForMap), 0.5f);
        Invoke(nameof(TriggerGeneratorGenerationForMap), 0.5f);
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

    void SetupGeneratorSpawner(GameObject mapRoot = null)
    {
        GameObject targetMap = mapRoot != null ? mapRoot : mapParent;

        if (targetMap == null)
            return;

        GenerateGenerators spawner = targetMap.AddComponent<GenerateGenerators>();
        spawner.generatorPrefab = generatorPrefab;
        spawner.generatorCount = 7;

        if (numberOfMaps <= 1)
        {
            generatorSpawner = spawner;
        }
    }

    void TriggerGeneratorGenerationForMap()
    {
        if (multiMapContainer != null)
        {
            GenerateGenerators[] spawners = multiMapContainer.GetComponentsInChildren<GenerateGenerators>();
            foreach (var spawner in spawners)
            {
                spawner.OnMapGenerationComplete();
            }
        }
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
        
        // Add MapEnvironmentController to each map
        MapEnvironmentController envController = mapParent.AddComponent<MapEnvironmentController>();
        envController.episodeTimeLimitSeconds = episodeTimeLimitSeconds;
        
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
            SetupGeneratorSpawner(mapParent);
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
    
    public void RegenerateSpecificMap(GameObject mapRoot)
    {
        if (mapRoot == null) return;
        
        // Get map index from name
        string mapName = mapRoot.name;
        int mapIndex = 0;
        if (mapName.StartsWith("Map_"))
        {
            string indexStr = mapName.Substring(4);
            if (!int.TryParse(indexStr, out mapIndex))
            {
                mapIndex = 0;
            }
        }
        
        // IMPORTANT: Store the current world position before destroying children
        Vector3 savedWorldPosition = mapRoot.transform.position;
        
        // Destroy all children except the MapEnvironmentController
        MapEnvironmentController envController = mapRoot.GetComponent<MapEnvironmentController>();
        foreach (Transform child in mapRoot.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Set this as the current mapParent for generation
        mapParent = mapRoot;
        
        // IMPORTANT: Reset the map parent position to zero before generating tiles
        // (tiles will be placed relative to the parent, then we move the parent)
        mapParent.transform.position = Vector3.zero;
        
        // Regenerate map contents at local origin
        RegenerateMapContents(Vector3.zero);
        
        // IMPORTANT: Restore the saved world position AFTER tiles are children
        mapParent.transform.position = savedWorldPosition;
        
        // Setup pallet and generator spawners
        SetupPalletGenerator(mapParent);
        SetupGeneratorSpawner(mapParent);
        
        // Trigger generation after a delay
        StartCoroutine(TriggerRegenerationForMap(mapParent));
    }
    
    private void RegenerateMapContents(Vector3 unusedOffset)
    {
        // Note: mapOffset is not used - tiles are placed relative to mapParent at origin,
        // then the parent is moved to the correct world position
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

               if (x == 0 && y==0)
                {
                    tilePrefab = bottomLeftCorner;
                    rotation = Quaternion.identity;
                }
                else if ( x == 0 && y == height - 1)
                {
                    tilePrefab = topLeftCorner;
                    rotation = Quaternion.identity;
                }
                else if (x == width -1  && y == height - 1)
                {
                    tilePrefab = topRightCorner;
                    rotation = Quaternion.identity;
                }
                else if (x == width - 1 && y == 0)
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
    }
    
    private System.Collections.IEnumerator TriggerRegenerationForMap(GameObject mapRoot)
    {
        yield return new WaitForSeconds(0.5f);
        
        GeneratePallets palletGen = mapRoot.GetComponent<GeneratePallets>();
        if (palletGen != null)
        {
            palletGen.OnMapGenerationComplete();
        }
        
        GenerateGenerators genSpawner = mapRoot.GetComponent<GenerateGenerators>();
        if (genSpawner != null)
        {
            genSpawner.OnMapGenerationComplete();
        }
    }
}