using UnityEngine;

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GenerateGenerators : MonoBehaviour
{
    [Header("Generator Prefab")]
    public GameObject generatorPrefab;
    
    [Header("Spawn Settings")]
    public int generatorCount = 7;
    
    private List<GeneratorSpawnMarker> allMarkers = new List<GeneratorSpawnMarker>();
    private int targetGeneratorCount;
    private Transform mapRoot;
    
    void Awake()
    {
        // Find the root map object (this component should be attached to it)
        mapRoot = transform;
    }
    
    public void OnMapGenerationComplete()
    {
        CollectAllMarkers();
        GenerateGeneratorsFromMarkers();
    }
    
    void CollectAllMarkers()
    {
        allMarkers.Clear();
        
        // Only collect markers that are children of THIS map instance
        GeneratorSpawnMarker[] markers = mapRoot.GetComponentsInChildren<GeneratorSpawnMarker>();
        allMarkers.AddRange(markers);
        
        Debug.Log($"[{mapRoot.name}] Found {allMarkers.Count} generator spawn markers");
    }
    
    void GenerateGeneratorsFromMarkers()
    {
        if (allMarkers.Count == 0)
        {
            Debug.LogWarning($"[{mapRoot.name}] No generator spawn markers found in this map!");
            return;
        }
        
        if (generatorPrefab == null)
        {
            Debug.LogError($"[{mapRoot.name}] Generator prefab is not assigned in GenerateGenerators!");
            return;
        }
        
        targetGeneratorCount = generatorCount;
        List<GeneratorSpawnMarker> selectedMarkers = new List<GeneratorSpawnMarker>();
        
        // Step 1: Spawn all guaranteed generators
        List<GeneratorSpawnMarker> guaranteed = allMarkers
            .Where(m => m.priority == GeneratorSpawnMarker.SpawnPriority.Guaranteed)
            .ToList();
        selectedMarkers.AddRange(guaranteed);
        selectedMarkers = selectedMarkers.Take(targetGeneratorCount).ToList(); // In case we have more guaranteed than target
        
        // Step 2: Handle VeryHigh priority groups (e.g., main building: at least 1 of 3)
        Dictionary<string, List<GeneratorSpawnMarker>> veryHighGroups = allMarkers
            .Where(m => m.priority == GeneratorSpawnMarker.SpawnPriority.VeryHigh && !string.IsNullOrEmpty(m.spawnGroup))
            .GroupBy(m => m.spawnGroup)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var group in veryHighGroups.Values)
        {
            if (group.Count > 0)
            {
                // Ensure at least one spawns from this group
                GeneratorSpawnMarker forced = GetWeightedRandom(group);
                selectedMarkers.Add(forced);
                group.Remove(forced);
                
                // Add remaining from group to weighted pool
                foreach (var marker in group)
                {
                    if (selectedMarkers.Count < targetGeneratorCount)
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
        List<GeneratorSpawnMarker> highPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == GeneratorSpawnMarker.SpawnPriority.High)
            .ToList();
        
        while (selectedMarkers.Count < targetGeneratorCount && highPriority.Count > 0)
        {
            GeneratorSpawnMarker selected = GetWeightedRandom(highPriority);
            selectedMarkers.Add(selected);
            highPriority.Remove(selected);
        }
        
        // Process Medium priority next
        List<GeneratorSpawnMarker> mediumPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == GeneratorSpawnMarker.SpawnPriority.Medium)
            .ToList();
        
        while (selectedMarkers.Count < targetGeneratorCount && mediumPriority.Count > 0)
        {
            GeneratorSpawnMarker selected = GetWeightedRandom(mediumPriority);
            selectedMarkers.Add(selected);
            mediumPriority.Remove(selected);
        }
        
        // Process Low priority last
        List<GeneratorSpawnMarker> lowPriority = allMarkers
            .Where(m => !selectedMarkers.Contains(m) && m.priority == GeneratorSpawnMarker.SpawnPriority.Low)
            .ToList();
        
        while (selectedMarkers.Count < targetGeneratorCount && lowPriority.Count > 0)
        {
            GeneratorSpawnMarker selected = GetWeightedRandom(lowPriority);
            selectedMarkers.Add(selected);
            lowPriority.Remove(selected);
        }
        
        // Step 4: If we have too many, trim (shouldn't happen with guaranteed, but just in case)
        if (selectedMarkers.Count > targetGeneratorCount)
        {
            // Remove lowest priority non-guaranteed generators
            var toRemove = selectedMarkers
                .Where(m => m.priority != GeneratorSpawnMarker.SpawnPriority.Guaranteed)
                .OrderByDescending(m => m.priority)
                .Take(selectedMarkers.Count - targetGeneratorCount)
                .ToList();
            
            foreach (var marker in toRemove)
            {
                selectedMarkers.Remove(marker);
            }
        }
        
        // Step 5: Spawn generators and destroy markers
        SpawnGenerators(selectedMarkers);
        DestroyAllMarkers();
        
        Debug.Log($"[{mapRoot.name}] Generated {selectedMarkers.Count} generators (target: {targetGeneratorCount})");
    }
    
    GeneratorSpawnMarker GetWeightedRandom(List<GeneratorSpawnMarker> markers)
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
    
    void SpawnGenerators(List<GeneratorSpawnMarker> markers)
    {
        GameObject generatorContainer = new GameObject("Generators");
        generatorContainer.transform.SetParent(mapRoot);
        generatorContainer.transform.localPosition = Vector3.zero;
        
        foreach (var marker in markers)
        {
            GameObject generator = Instantiate(generatorPrefab, marker.transform.position, marker.transform.rotation, generatorContainer.transform);
            generator.name = $"Generator_{marker.priority}";
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