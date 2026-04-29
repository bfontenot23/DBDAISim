using UnityEngine;

public class PrefabMapInitializer : MonoBehaviour
{
    [Header("Pallet Generation")]
    public GameObject palletPrefab;
    
    [Range(10, 20)]
    public int minPallets = 10;
    
    [Range(10, 20)]
    public int maxPallets = 20;
    
    [Header("Timing")]
    [Tooltip("Delay before generating pallets (to ensure all children are initialized)")]
    public float generationDelay = 0.1f;
    
    private GeneratePallets palletGenerator;
    
    void Start()
    {
        SetupPalletGenerator();
        
        // Delay pallet generation to ensure all children are initialized
        if (palletGenerator != null)
        {
            Invoke(nameof(TriggerPalletGeneration), generationDelay);
        }
    }
    
    void SetupPalletGenerator()
    {
        // Add GeneratePallets component to this map root
        palletGenerator = gameObject.AddComponent<GeneratePallets>();
        palletGenerator.palletPrefab = palletPrefab;
        palletGenerator.minPallets = minPallets;
        palletGenerator.maxPallets = maxPallets;
    }
    
    void TriggerPalletGeneration()
    {
        if (palletGenerator != null)
        {
            palletGenerator.OnMapGenerationComplete();
        }
    }
    
    /// <summary>
    /// Call this manually if you need to regenerate pallets at runtime
    /// </summary>
    public void RegeneratePallets()
    {
        if (palletGenerator != null)
        {
            // Clear existing pallets
            Transform palletsContainer = transform.Find("Pallets");
            if (palletsContainer != null)
            {
                Destroy(palletsContainer.gameObject);
            }
            
            palletGenerator.OnMapGenerationComplete();
        }
    }
}
