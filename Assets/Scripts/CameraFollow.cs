using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public MapGenerator mapGenerator;

    [Header("Camera Settings")]
    public float transitionSpeed = 5f;
    public float zoomSpeed = 5f;
    [Tooltip("Extra padding (in world units) added when fitting a map or the full grid on screen")]
    public float fitPadding = 8f;

    [Header("State (Read-Only in Inspector)")]
    public int activeMapIndex = 0;

    private Camera cam;
    private Vector3 targetPosition;
    private float targetOrthoSize;

    private bool overviewMode = false;

    // Derived from MapGenerator
    private float mapWorldWidth;
    private float mapWorldHeight;
    private int gridWidth;
    private int gridHeight;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        if (mapGenerator != null)
        {
            mapWorldWidth  = mapGenerator.width  * 16f;
            mapWorldHeight = mapGenerator.height * 16f;

            int[] dims = CalculateGridDimensions(mapGenerator.numberOfMaps);
            gridWidth  = dims[0];
            gridHeight = dims[1];
        }

        // Snap immediately to Map_0 on start
        Vector3 startPos = GetMapCenter(activeMapIndex);
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
        cam.orthographicSize = GetSizeForMap();
        targetPosition = transform.position;
        targetOrthoSize = cam.orthographicSize;
    }

    void Update()
    {
        HandleInput();

        // Smooth position
        transform.position = Vector3.Lerp(transform.position, targetPosition, transitionSpeed * Time.deltaTime);

        // Smooth zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomSpeed * Time.deltaTime);
    }

    void HandleInput()
    {
        if (mapGenerator == null) return;
        int total = mapGenerator.numberOfMaps;

        // Tab toggles overview mode
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            overviewMode = !overviewMode;
            if (overviewMode)
            {
                SetOverviewTarget();
            }
            else
            {
                SetMapTarget(activeMapIndex);
            }
            return;
        }

        if (overviewMode) return;

        // Arrow key cycling - horizontal wraps within the same row, vertical moves rows
        int[] dims = CalculateGridDimensions(total);
        int cols = dims[0];

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int next = activeMapIndex + 1;
            if (next < total) SetMapTarget(next);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int next = activeMapIndex - 1;
            if (next >= 0) SetMapTarget(next);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int next = activeMapIndex + cols;
            if (next < total) SetMapTarget(next);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int next = activeMapIndex - cols;
            if (next >= 0) SetMapTarget(next);
        }
    }

    void SetMapTarget(int index)
    {
        activeMapIndex = index;
        overviewMode = false;
        Vector3 center = GetMapCenter(index);
        targetPosition = new Vector3(center.x, center.y, transform.position.z);
        targetOrthoSize = GetSizeForMap();
    }

    void SetOverviewTarget()
    {
        if (mapGenerator == null) return;

        int total = mapGenerator.numberOfMaps;
        int[] dims = CalculateGridDimensions(total);
        int cols = dims[0];
        int rows = dims[1];

        float spacingX = mapGenerator.mapSpacing * 16f;
        float spacingY = mapGenerator.mapSpacing * 16f;

        // Full grid extents
        float gridTotalWidth  = cols * mapWorldWidth  + (cols - 1) * spacingX;
        float gridTotalHeight = rows * mapWorldHeight + (rows - 1) * spacingY;

        // Center of the grid: Map_0 is centered at (0,0), grid goes right and down
        float centerX = (gridTotalWidth  - mapWorldWidth)  / 2f;
        float centerY = -((gridTotalHeight - mapWorldHeight) / 2f);

        targetPosition = new Vector3(centerX, centerY, transform.position.z);
        targetOrthoSize = GetSizeForGrid(gridTotalWidth, gridTotalHeight);
    }

    // Returns the world-space center of the map at the given index
    Vector3 GetMapCenter(int index)
    {
        if (mapGenerator == null) return Vector3.zero;

        int[] dims = CalculateGridDimensions(mapGenerator.numberOfMaps);
        int cols = dims[0];

        float spacingX = mapGenerator.mapSpacing * 16f;
        float spacingY = mapGenerator.mapSpacing * 16f;

        int col = index % cols;
        int row = index / cols;

        float x = col * (mapWorldWidth  + spacingX);
        float y = -row * (mapWorldHeight + spacingY);

        return new Vector3(x, y, 0f);
    }

    // Orthographic size to fit one map on screen with padding
    float GetSizeForMap()
    {
        if (cam == null) return 1f;

        float aspect = cam.aspect;
        float sizeFromHeight = (mapWorldHeight / 2f) + fitPadding;
        float sizeFromWidth  = (mapWorldWidth  / 2f) / aspect + fitPadding;
        return Mathf.Max(sizeFromHeight, sizeFromWidth);
    }

    // Orthographic size to fit the entire grid on screen with padding
    float GetSizeForGrid(float gridTotalWidth, float gridTotalHeight)
    {
        if (cam == null) return 1f;

        float aspect = cam.aspect;
        float sizeFromHeight = (gridTotalHeight / 2f) + fitPadding;
        float sizeFromWidth  = (gridTotalWidth  / 2f) / aspect + fitPadding;
        return Mathf.Max(sizeFromHeight, sizeFromWidth);
    }

    int[] CalculateGridDimensions(int numMaps)
    {
        if (numMaps <= 1)
            return new int[] { 1, 1 };

        int cols = Mathf.CeilToInt(Mathf.Sqrt(numMaps));
        int rows = Mathf.CeilToInt((float)numMaps / cols);
        return new int[] { cols, rows };
    }
}
