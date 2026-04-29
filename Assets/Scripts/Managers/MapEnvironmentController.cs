using UnityEngine;
using Unity.MLAgents;

public class MapEnvironmentController : MonoBehaviour
{
    [Header("Agent Group")]
    private SimpleMultiAgentGroup agentGroup;
    
    [Header("Episode Settings")]
    public float episodeTimeLimitSeconds = 600f;
    private float episodeTimer = 0f;
    
    [Header("Map References")]
    private Transform mapRoot;
    private SurvivorAgent[] survivors;
    private KillerAgent killer;
    
    [Header("Map Generation")]
    private MapGenerator mapGenerator;
    private bool isEpisodeActive = false;
    
    void Awake()
    {
        agentGroup = new SimpleMultiAgentGroup();
        mapRoot = transform;
    }
    
    void Start()
    {
        StartCoroutine(DelayedAgentRegistration());
    }
    
    private System.Collections.IEnumerator DelayedAgentRegistration()
    {
        // Wait for markers to spawn agents
        yield return new WaitForSeconds(0.2f);
        
        RegisterAgents();
    }
    
    void Update()
    {
        if (!isEpisodeActive) return;
        
        // Track episode time
        episodeTimer += Time.deltaTime;
        if (episodeTimer >= episodeTimeLimitSeconds)
        {
            HandleTimeLimit();
        }
    }
    
    public void RegisterAgents()
    {
        // Clear existing agents from the group
        if (agentGroup == null)
        {
            agentGroup = new SimpleMultiAgentGroup();
        }
        
        // Find all agents in this map
        survivors = GetComponentsInChildren<SurvivorAgent>();
        killer = GetComponentInChildren<KillerAgent>();
        
        // Unregister any old agents (in case of regeneration)
        // Note: SimpleMultiAgentGroup doesn't have an explicit unregister method,
        // so we create a new group instance
        agentGroup = new SimpleMultiAgentGroup();
        
        // Register all agents in the group
        int registeredCount = 0;
        foreach (var survivor in survivors)
        {
            if (survivor != null)
            {
                agentGroup.RegisterAgent(survivor);
                registeredCount++;
            }
        }
        
        if (killer != null)
        {
            agentGroup.RegisterAgent(killer);
            registeredCount++;
        }
        
        Debug.Log($"[MapEnvironmentController] Registered {registeredCount} agents in {gameObject.name}");
        
        isEpisodeActive = true;
        episodeTimer = 0f;
    }
    
    public void EndEnvironmentEpisode()
    {
        if (!isEpisodeActive) return;
        
        isEpisodeActive = false;
        
        // End episode for all agents in the group at once
        agentGroup.GroupEpisodeInterrupted();
        
        RegenerateMap();
    }
    
    public void AddGroupReward(float reward)
    {
        agentGroup.AddGroupReward(reward);
    }
    
    public void SetGroupReward(float reward)
    {
        agentGroup.SetGroupReward(reward);
    }
    
    private void HandleTimeLimit()
    {
        // Penalize all agents for time limit
        foreach (var survivor in survivors)
        {
            if (survivor != null && !survivor.isEliminated)
            {
                survivor.AddReward(-20.0f);
            }
        }
        
        if (killer != null)
        {
            killer.AddReward(-20.0f);
        }
        
        EndEnvironmentEpisode();
    }
    
    public void OnAllSurvivorsEliminated()
    {
        // Reward killer
        if (killer != null)
        {
            killer.AddReward(10.0f);
        }
        
        
        EndEnvironmentEpisode();
    }
    
    public void OnAllSurvivorsDying()
    {
        Debug.Log($"[MapEnvironmentController] OnAllSurvivorsDying called for {gameObject.name}");
        
        // Reward killer for getting all survivors down
        if (killer != null)
        {
            killer.AddReward(10.0f);
            Debug.Log($"[MapEnvironmentController] Rewarded killer +10.0");
        }
        else
        {
            Debug.LogWarning($"[MapEnvironmentController] Killer is null!");
        }
        
        // Penalize survivors
        int penalizedCount = 0;
        foreach (var survivor in survivors)
        {
            if (survivor != null && !survivor.isEliminated)
            {
                survivor.AddReward(-3.0f);
                penalizedCount++;
            }
        }
        Debug.Log($"[MapEnvironmentController] Penalized {penalizedCount} survivors -3.0");
        
        Debug.Log($"[MapEnvironmentController] Ending episode for {gameObject.name}");
        EndEnvironmentEpisode();
    }
    
    public void OnSurvivorsEscape()
    {
        // This would be called when survivors complete generators and escape
        // Reward survivors
        foreach (var survivor in survivors)
        {
            if (survivor != null && !survivor.isEliminated)
            {
                survivor.AddReward(5.0f);
            }
        }
        
        // Penalize killer
        if (killer != null)
        {
            killer.AddReward(-10.0f);
        }
        
        EndEnvironmentEpisode();
    }
    
    private void RegenerateMap()
    {
        // Get the MapGenerator from parent or scene
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();  // i know its depreciated but i like this function
        }
        
        if (mapGenerator != null)
        {
            // Destroy all agents first
            if (survivors != null)
            {
                foreach (var survivor in survivors)
                {
                    if (survivor != null)
                    {
                        Destroy(survivor.gameObject);
                    }
                }
            }
            
            if (killer != null)
            {
                Destroy(killer.gameObject);
            }
            
            // Clear references
            survivors = null;
            killer = null;
            
            // Destroy current map contents (keep the root)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            
            // Wait a frame then regenerate
            StartCoroutine(RegenerateMapCoroutine());
        }
        else
        {
            Debug.LogWarning("[MapEnvironmentController] MapGenerator not found. Cannot regenerate map.");
            // Just reset the episode without regenerating
            StartCoroutine(ResetEpisodeCoroutine());
        }
    }
    
    private System.Collections.IEnumerator RegenerateMapCoroutine()
    {
        // Wait for cleanup
        yield return new WaitForEndOfFrame();
        
        // Regenerate the map for this environment
        if (mapGenerator != null)
        {
            mapGenerator.RegenerateSpecificMap(gameObject);
        }
        
        // Wait for generation and agent spawning to complete
        yield return new WaitForSeconds(1.2f);
        
        // Re-register agents (they will be newly spawned from markers)
        RegisterAgents();
    }
    
    private System.Collections.IEnumerator ResetEpisodeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        
        // Just reset the timer and reactivate
        episodeTimer = 0f;
        isEpisodeActive = true;
        
        // Re-register agents
        RegisterAgents();
    }
    
    public bool IsEpisodeActive()
    {
        return isEpisodeActive;
    }
    
    public float GetEpisodeTime()
    {
        return episodeTimer;
    }
}
