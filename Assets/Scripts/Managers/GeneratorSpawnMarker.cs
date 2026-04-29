using UnityEngine;

public class GeneratorSpawnMarker : MonoBehaviour
{
    public enum SpawnPriority
    {
        Guaranteed = 0,      // Always spawns (shack, 1 in main building)
        VeryHigh = 1,        // Main building secondary (at least 1 of 3)
        High = 2,            // Jungle gyms
        Medium = 3,          // Other structures
        Low = 4              // Filler tiles
    }

    [Header("Spawn Settings")]
    public SpawnPriority priority = SpawnPriority.Medium;
    
    [Tooltip("Higher = more likely to be chosen")]
    [Range(0f, 1f)]
    public float weight = 1f;

    [Tooltip("For guaranteed groups (e.g., main building has 1 guaranteed + 3 where at least 1 must spawn)")]
    public string spawnGroup = "";

    void OnDrawGizmos()
    {
        Gizmos.color = GetPriorityColor();
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }

   Color GetPriorityColor()
    {
        switch (priority)
        {
            case SpawnPriority.Guaranteed:
                return Color.green;
            case SpawnPriority.VeryHigh:
                return Color.cyan;
            case SpawnPriority.High:
                return Color.yellow;
            case SpawnPriority.Medium:
                return new Color(1f, 0.5f, 0f);
            case SpawnPriority.Low:
                return Color.red;
            default:
                return Color.white;
        }
    } 
}
