using UnityEngine;

public class GenerateFillerTile : MonoBehaviour
{
    [Header("Filler Tile Prefabs")]
    public GameObject[] fillerTilePrefabs;

    void Awake()
    {
        GenerateAndReplace();
    }

    void GenerateAndReplace()
    {
        if (fillerTilePrefabs == null || fillerTilePrefabs.Length == 0)
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
        if (transform.parent != null)
        {
            if (transform.parent.name == "GenFiller")
            {
                return transform.parent.parent;
            }
            else if (transform.parent.name.StartsWith("Gen_Filler"))
            {
                Transform potentialMapPrefab = transform.parent.parent;
                if (potentialMapPrefab != null && potentialMapPrefab.name.Contains("Map"))
                {
                    return potentialMapPrefab;
                }
            }
        }
        return null;
    }

    GameObject GetRandomPrefab()
    {
        int randomIndex = Random.Range(0, fillerTilePrefabs.Length);
        return fillerTilePrefabs[randomIndex];
    }

    Quaternion GetRandomRotation()
    {
        int randomRotation = Random.Range(0, 4);
        float angle = randomRotation * 90f;
        return Quaternion.Euler(0, 0, angle);
    }
}

