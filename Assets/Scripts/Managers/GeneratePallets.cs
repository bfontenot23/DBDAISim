using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GeneratePallets : MonoBehaviour
{
    [Header("Pallet Prefab")]
    public GameObject palletPrefab;
    
    [Header("Spawn Settings")]
    [Range(10, 20)]
    public int minPallets = 10;
    [Range(10, 20)]
    public int maxPallets = 20;
    
    private List<PalletSpawnMarker> allMarkers = new List<PalletSpawnMarker>();
    private int targetPalletCount;
    private Transform mapRoot;
    
    void Awake()
    {
        // Find the root map object (this component should be attached to it)
        mapRoot = transform;
    }
    
    public void OnMapGenerationComplete()
    {
        CollectAllMarkers();
        GeneratePalletsFromMarkers();
    }
    
    void CollectAllMarkers()
    {
        allMarkers.Clear();
        
        // Only collect markers that are children of THIS map instance
        PalletSpawnMarker[] markers = mapRoot.GetComponentsInChildren<PalletSpawnMarker>();
        allMarkers.AddRange(markers);
        
        Debug.Log($"[{mapRoot.name}] Found {allMarkers.Count} pallet spawn markers");
    }
    
    void GeneratePalletsFromMarkers()
    {
        if (allMarkers.Count == 0)
        {
            Debug.LogWarning($"[{mapRoot.name}] No pallet spawn markers found in this map!");
            return;
        }
        
        if (palletPrefab == null)
        {
            Debug.LogError($"[{mapRoot.name}] Pallet prefab is not assigned in GeneratePallets!");
            return;
        }
        
        targetPalletCount = Random.Range(minPallets, maxPallets + 1);
        List<PalletSpawnMarker> selectedMarkers = new List<PalletSpawnMarker>();
        
        // Step 1: Spawn all guaranteed pallets
        List<PalletSpawnMarker> guaranteed = allMarkers
            .Where(m => m.priority == PalletSpawnMarker.SpawnPriority.Guaranteed)
            .ToList();
        selectedMarkers.AddRange(guaranteed);
        
        // Step 2: Handle VeryHigh priority groups (e.g., main building: at least 1 of 3)
        Dictionary<string, List<PalletSpawnMarker>> veryHighGroups = allMarkers
            .Where(m => m.priority == PalletSpawnMarker.SpawnPriority.VeryHigh && !string.IsNullOrEmpty(m.spawnGroup))
            .GroupBy(m => m.spawnGroup)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var group in veryHighGroups.Values)
        {
            if (group.Count > 0)
            {
                // Ensure at least one spawns from this group
                PalletSpawnMarker forced = GetWeightedRandom(group);
                selectedMarkers.Add(forced);
                group.Remove(forced);
                
                // Add remaining from group to weighted pool
                foreach (var marker in group)
                {
                    if (selectedMarkers.Count < targetPalletCount)
                    {
                        float chance = marker.weight;
                        if (Random.value < chance)
                        {
                            selectedMarkers.Add(marker);
                        }
                    }
                }
            }
        }
        
        // Step 3: Fill remaining slots with weighted selection from High, then Medium, then Low
        // Process High priority first
        List<PalletSpawnMarker> highPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == PalletSpawnMarker.SpawnPriority.High)
            .ToList();
        
        while (selectedMarkers.Count < targetPalletCount && highPriority.Count > 0)
        {
            PalletSpawnMarker selected = GetWeightedRandom(highPriority);
            selectedMarkers.Add(selected);
            highPriority.Remove(selected);
        }
        
        // Process Medium priority next
        List<PalletSpawnMarker> mediumPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == PalletSpawnMarker.SpawnPriority.Medium)
            .ToList();
        
        while (selectedMarkers.Count < targetPalletCount && mediumPriority.Count > 0)
        {
            PalletSpawnMarker selected = GetWeightedRandom(mediumPriority);
            selectedMarkers.Add(selected);
            mediumPriority.Remove(selected);
        }
        
        // Process Low priority last
        List<PalletSpawnMarker> lowPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == PalletSpawnMarker.SpawnPriority.Low)
            .ToList();
        
        while (selectedMarkers.Count < targetPalletCount && lowPriority.Count > 0)
        {
            PalletSpawnMarker selected = GetWeightedRandom(lowPriority);
            selectedMarkers.Add(selected);
            lowPriority.Remove(selected);
        }
        
        // Step 4: If we have too many, trim (shouldn't happen with guaranteed, but just in case)
        if (selectedMarkers.Count > targetPalletCount)
        {
            // Remove lowest priority non-guaranteed pallets
            var toRemove = selectedMarkers
                .Where(m => m.priority != PalletSpawnMarker.SpawnPriority.Guaranteed)
                .OrderByDescending(m => m.priority)
                .Take(selectedMarkers.Count - targetPalletCount)
                .ToList();
            
            foreach (var marker in toRemove)
            {
                selectedMarkers.Remove(marker);
            }
        }
        
        // Step 5: Spawn pallets and destroy markers
        SpawnPallets(selectedMarkers);
        DestroyAllMarkers();
        
        Debug.Log($"[{mapRoot.name}] Generated {selectedMarkers.Count} pallets (target: {targetPalletCount})");
    }
    
    PalletSpawnMarker GetWeightedRandom(List<PalletSpawnMarker> markers)
    {
        if (markers.Count == 0)
            return null;
        
        if (markers.Count == 1)
            return markers[0];
        
        float totalWeight = markers.Sum(m => m.weight);
        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;
        
        foreach (var marker in markers)
        {
            cumulative += marker.weight;
            if (randomValue <= cumulative)
            {
                return marker;
            }
        }
        
        return markers[markers.Count - 1];
    }
    
    void SpawnPallets(List<PalletSpawnMarker> markers)
    {
        GameObject palletContainer = new GameObject("Pallets");
        palletContainer.transform.SetParent(mapRoot);
        palletContainer.transform.localPosition = Vector3.zero;
        
        foreach (var marker in markers)
        {
            GameObject pallet = Instantiate(palletPrefab, marker.transform.position, marker.transform.rotation, palletContainer.transform);
            pallet.name = $"Pallet_{marker.priority}";
        }
    }
    
    void DestroyAllMarkers()
    {
        foreach (var marker in allMarkers)
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }
        allMarkers.Clear();
    }
}
