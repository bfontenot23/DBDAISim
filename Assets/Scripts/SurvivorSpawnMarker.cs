using UnityEngine;

public class SurvivorSpawnMarker : MonoBehaviour
{
    [Header("Survivor Prefab")]
    public GameObject survivorPrefab;
    
    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    
    private GameObject spawnedSurvivor;
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnSurvivor();
        }
    }
    
    public void SpawnSurvivor()
    {
        if (survivorPrefab == null)
        {
            Debug.LogError("[SurvivorSpawnMarker] Survivor prefab not assigned!");
            return;
        }
        
        // Don't spawn if already spawned
        if (spawnedSurvivor != null)
        {
            Debug.LogWarning("[SurvivorSpawnMarker] Survivor already spawned at this marker.");
            return;
        }
        
        // Find the Map (grandparent or higher)
        Transform mapTransform = FindMapParent();
        if (mapTransform == null)
        {
            Debug.LogError("[SurvivorSpawnMarker] Could not find Map parent!");
            return;
        }
        
        // Spawn survivor at marker position and rotation
        spawnedSurvivor = Instantiate(survivorPrefab, transform.position, transform.rotation);
        
        // Parent directly to the Map (not the tile)
        spawnedSurvivor.transform.SetParent(mapTransform);
        
        Debug.Log($"[SurvivorSpawnMarker] Spawned survivor in {mapTransform.name} at position {transform.position}");
        
        // Destroy the marker after spawning
        Destroy(gameObject);
    }
    
    private Transform FindMapParent()
    {
        Transform current = transform.parent;
        
        // Keep going up until we find a transform with "Map" in its name or reach the root
        while (current != null)
        {
            if (current.name.Contains("Map") || current.parent == null)
            {
                return current;
            }
            current = current.parent;
        }
        
        return null;
    }
    
    public GameObject GetSpawnedSurvivor()
    {
        return spawnedSurvivor;
    }
}

