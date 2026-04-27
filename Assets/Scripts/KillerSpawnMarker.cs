using UnityEngine;

public class KillerSpawnMarker : MonoBehaviour
{
    [Header("Killer Prefab")]
    public GameObject killerPrefab;
    
    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    
    private GameObject spawnedKiller;
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnKiller();
        }
    }
    
    public void SpawnKiller()
    {
        if (killerPrefab == null)
        {
            Debug.LogError("[KillerSpawnMarker] Killer prefab not assigned!");
            return;
        }
        
        // Don't spawn if already spawned
        if (spawnedKiller != null)
        {
            Debug.LogWarning("[KillerSpawnMarker] Killer already spawned at this marker.");
            return;
        }
        
        // Find the Map (grandparent or higher)
        Transform mapTransform = FindMapParent();
        if (mapTransform == null)
        {
            Debug.LogError("[KillerSpawnMarker] Could not find Map parent!");
            return;
        }
        
        // Check if a killer already exists in this map
        KillerAgent existingKiller = mapTransform.GetComponentInChildren<KillerAgent>();
        if (existingKiller != null)
        {
            Debug.LogWarning("[KillerSpawnMarker] A killer already exists in this map. Skipping spawn.");
            Destroy(gameObject);
            return;
        }
        
        // Spawn killer at marker position and rotation
        spawnedKiller = Instantiate(killerPrefab, transform.position, transform.rotation);
        
        // Parent directly to the Map (not the tile)
        spawnedKiller.transform.SetParent(mapTransform);
        
        Debug.Log($"[KillerSpawnMarker] Spawned killer in {mapTransform.name} at position {transform.position}");
        
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
    
    public GameObject GetSpawnedKiller()
    {
        return spawnedKiller;
    }
}

