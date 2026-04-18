using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject wallPrefab;

    public int width = 50;
    public int height = 50;
    public int wallCount = 50;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("MapGenerator running");
        GenerateMap();
        CreateBorderWalls();
    }

    void GenerateMap()
    {
        //space between tiles
        float tileSize = 1f;

        for (int x = -width / 2; x < width / 2; x++)
        {
            for (int y = -height / 2; y < height / 2; y++)
            {
                Vector3 position = new Vector3(x * tileSize, y * tileSize, 0);
                //random walls
                if (Random.value < 0.2f)
                {
                    Instantiate(wallPrefab, position, Quaternion.identity);
                }
            }
        }   
    }
    void CreateBorderWalls()
    {
        for (int x = -width / 2; x <= width / 2; x++)
        {
            Instantiate(wallPrefab, new Vector3(x, -height / 2, 0), Quaternion.identity); //bottom border
            Instantiate(wallPrefab, new Vector3(x, height / 2, 0), Quaternion.identity); // top border
        }
        for (int y = -height / 2; y <= height / 2; y++)
        {
            Instantiate(wallPrefab, new Vector3(-width / 2, y, 0), Quaternion.identity); // left border
            Instantiate(wallPrefab, new Vector3(width / 2, y, 0), Quaternion.identity); //right border
        }
    }
}