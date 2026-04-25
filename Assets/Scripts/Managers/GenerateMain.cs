using UnityEngine;

public class GenerateMain : MonoBehaviour
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
        
        Transform mapPrefab = GetMapPrefabParent();
        if (mapPrefab != null)
        {
            newTile.transform.SetParent(mapPrefab);
        }
        
        Destroy(gameObject);
    }

    Transform GetMapPrefabParent()
    {
        if (transform.parent != null && transform.parent.name.Contains("Map"))
        {
            return transform.parent;
        }
        return null;
    }

    GameObject GetRandomPrefab()
    {
        int randomIndex = Random.Range(0, mazeTilePrefabs.Length);
        return mazeTilePrefabs[randomIndex];
    }

    Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, 0, 0);
    }
}

