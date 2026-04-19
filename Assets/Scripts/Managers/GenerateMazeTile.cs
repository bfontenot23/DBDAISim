using UnityEngine;

public class GenerateMazeTile : MonoBehaviour
{
    [Header("Maze Tile Prefabs")]
    public GameObject[] mazeTilePrefabs;

    void Awake()
    {
        GenerateAndReplace();
    }

    void GenerateAndReplace()
    {
        if (mazeTilePrefabs == null || mazeTilePrefabs.Length == 0)
        {
            Debug.LogWarning("GenerateFillerTile: No filler tile prefabs assigned!");
            Destroy(gameObject);
            return;
        }

        GameObject selectedPrefab = GetRandomPrefab();
        
        if (selectedPrefab == null)
        {
            Debug.LogWarning("GenerateFillerTile: Selected prefab is null!");
            Destroy(gameObject);
            return;
        }

        Quaternion randomRotation = GetRandomRotation();
        
        GameObject newTile = Instantiate(selectedPrefab);
        newTile.transform.position = transform.position;
        newTile.transform.rotation = randomRotation;
        
        Destroy(gameObject);
    }

    GameObject GetRandomPrefab()
    {
        int randomIndex = Random.Range(0, mazeTilePrefabs.Length);
        return mazeTilePrefabs[randomIndex];
    }

    Quaternion GetRandomRotation()
    {
        int randomRotation = Random.Range(0, 4);
        float angle = randomRotation * 90f;
        return Quaternion.Euler(0, 0, angle);
    }
}
