using UnityEngine;
using System.Collections.Generic;

public class ChaseManager : MonoBehaviour
{
    [Header("Chase Detection Settings")]
    public float chaseStartDistance = 16f;
    public float chaseEndDistance = 36f;
    public float chaseEndNoLOSTime = 8f;
    
    [Header("Chase Rewards")]
    public float chaseStartKillerReward = 1.0f;
    public float chaseDurationKillerPenaltyPerSecond = 0f;
    public float chaseDurationSurvivorRewardPerSecond = 0.1f;
    public float chaseLostKillerPenalty = -1.0f;
    public float chaseLostSurvivorReward = 1.0f;
    
    private KillerAgent killerAgent;
    private Transform killerTransform;
    private Transform environmentRoot;
    
    private HashSet<SurvivorAgent> survivorsInChase = new HashSet<SurvivorAgent>();
    private Dictionary<SurvivorAgent, float> noLOSTimers = new Dictionary<SurvivorAgent, float>();
    
    private LayerMask visionLayerMask;
    
    void Start()
    {
        // Find killer and environment root
        killerAgent = GetComponent<KillerAgent>();
        if (killerAgent == null)
        {
            Debug.LogError("[ChaseManager] KillerAgent component not found!");
            return;
        }
        
        killerTransform = killerAgent.transform;
        environmentRoot = killerTransform.parent;
        
        // Use the same layer mask as the killer's raycasts
        visionLayerMask = killerAgent.raycastLayerMask;
    }
    
    void Update()
    {
        if (killerAgent == null || environmentRoot == null) return;
        
        // Get all survivors in this map
        SurvivorAgent[] allSurvivors = environmentRoot.GetComponentsInChildren<SurvivorAgent>();
        
        // Track which survivors should be in chase
        HashSet<SurvivorAgent> shouldBeInChase = new HashSet<SurvivorAgent>();
        
        foreach (SurvivorAgent survivor in allSurvivors)
        {
            // Skip eliminated or dying survivors
            if (survivor.isEliminated || !survivor.gameObject.activeInHierarchy)
                continue;
            
            float distance = Vector2.Distance(killerTransform.position, survivor.transform.position);
            
            // Check if chase should start
            if (!survivorsInChase.Contains(survivor))
            {
                // Start chase conditions: within 16m AND has LOS
                if (distance <= chaseStartDistance && HasLineOfSight(survivor))
                {
                    StartChase(survivor);
                    shouldBeInChase.Add(survivor);
                }
            }
            else
            {
                // Chase is active - check if it should continue
                bool hasLOS = HasLineOfSight(survivor);
                bool hasLOSToScratchMarks = HasLineOfSightToScratchMarks(survivor);
                bool tooFar = distance > chaseEndDistance;
                bool isDying = survivor.GetHealthState() == SurvivorHealthState.Dying;
                
                // End chase if survivor is downed
                if (isDying)
                {
                    EndChase(survivor, true); // true = survivor downed
                    continue;
                }
                
                // End chase if too far
                if (tooFar)
                {
                    EndChase(survivor, false);
                    continue;
                }
                
                // Handle no LOS timer
                if (hasLOS || hasLOSToScratchMarks)
                {
                    // Reset timer if we have LOS or see scratch marks
                    noLOSTimers[survivor] = 0f;
                    shouldBeInChase.Add(survivor);
                }
                else
                {
                    // Increment no LOS timer
                    if (!noLOSTimers.ContainsKey(survivor))
                        noLOSTimers[survivor] = 0f;
                    
                    noLOSTimers[survivor] += Time.deltaTime;
                    
                    // End chase if no LOS for too long
                    if (noLOSTimers[survivor] >= chaseEndNoLOSTime)
                    {
                        EndChase(survivor, false);
                        continue;
                    }
                    else
                    {
                        shouldBeInChase.Add(survivor);
                    }
                }
                
                // Apply ongoing chase rewards/penalties
                if (shouldBeInChase.Contains(survivor))
                {
                    ApplyChaseRewards(survivor);
                }
            }
        }
        
        // Clean up survivors that left chase but weren't explicitly ended
        List<SurvivorAgent> toRemove = new List<SurvivorAgent>();
        foreach (SurvivorAgent survivor in survivorsInChase)
        {
            if (!shouldBeInChase.Contains(survivor))
            {
                toRemove.Add(survivor);
            }
        }
        
        foreach (SurvivorAgent survivor in toRemove)
        {
            if (survivorsInChase.Contains(survivor))
            {
                EndChase(survivor, false);
            }
        }
    }
    
    private bool HasLineOfSight(SurvivorAgent survivor)
    {
        Vector2 directionToSurvivor = survivor.transform.position - killerTransform.position;
        float distance = directionToSurvivor.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(killerTransform.position, directionToSurvivor.normalized, distance, visionLayerMask);
        
        if (hit.collider != null)
        {
            // Check if we hit the survivor directly
            if (hit.collider.gameObject == survivor.gameObject)
            {
                return true;
            }
            // Check if we only hit a ShortWall (can see through it)
            else if (hit.collider.CompareTag("ShortWall"))
            {
                RaycastHit2D[] allHits = Physics2D.RaycastAll(killerTransform.position, directionToSurvivor.normalized, distance, visionLayerMask);
                foreach (RaycastHit2D h in allHits)
                {
                    if (h.collider.gameObject == survivor.gameObject)
                    {
                        return true;
                    }
                    else if (h.collider.CompareTag("Wall"))
                    {
                        return false;
                    }
                }
            }
        }
        
        return false;
    }
    
    private bool HasLineOfSightToScratchMarks(SurvivorAgent survivor)
    {
        // Find scratch marks near the survivor's last known position
        ScratchMarks[] scratchMarks = FindObjectsOfType<ScratchMarks>();
        
        float scratchMarkDetectionRange = 10f; // Look for scratch marks within 10m of killer
        
        foreach (ScratchMarks mark in scratchMarks)
        {
            float distanceToMark = Vector2.Distance(killerTransform.position, mark.transform.position);
            
            if (distanceToMark <= scratchMarkDetectionRange)
            {
                // Check if we have LOS to this scratch mark
                Vector2 directionToMark = mark.transform.position - killerTransform.position;
                RaycastHit2D hit = Physics2D.Raycast(killerTransform.position, directionToMark.normalized, distanceToMark, visionLayerMask);
                
                if (hit.collider != null)
                {
                    if (hit.collider.gameObject == mark.gameObject)
                    {
                        return true;
                    }
                    else if (hit.collider.CompareTag("ShortWall"))
                    {
                        // Can see through short walls
                        RaycastHit2D[] allHits = Physics2D.RaycastAll(killerTransform.position, directionToMark.normalized, distanceToMark, visionLayerMask);
                        foreach (RaycastHit2D h in allHits)
                        {
                            if (h.collider.gameObject == mark.gameObject)
                            {
                                return true;
                            }
                            else if (h.collider.CompareTag("Wall"))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        return false;
    }
    
    private void StartChase(SurvivorAgent survivor)
    {
        survivorsInChase.Add(survivor);
        noLOSTimers[survivor] = 0f;
        
        // Reward killer for starting chase
        killerAgent.AddReward(chaseStartKillerReward);
        
        // No penalty/reward for survivor when chase starts
    }
    
    private void EndChase(SurvivorAgent survivor, bool survivorDowned)
    {
        if (!survivorsInChase.Contains(survivor))
            return;
        
        survivorsInChase.Remove(survivor);
        
        if (noLOSTimers.ContainsKey(survivor))
            noLOSTimers.Remove(survivor);
        
        // Only apply lost chase penalties if survivor wasn't downed
        if (!survivorDowned)
        {
            killerAgent.AddReward(chaseLostKillerPenalty);
            survivor.AddReward(chaseLostSurvivorReward);
        }
        // If survivor was downed, killer already got reward from hitting them
    }
    
    private void ApplyChaseRewards(SurvivorAgent survivor)
    {
        // Apply per-frame rewards scaled by deltaTime to get per-second rates
        float killerReward = chaseDurationKillerPenaltyPerSecond * Time.deltaTime;
        float survivorReward = chaseDurationSurvivorRewardPerSecond * Time.deltaTime;
        
        killerAgent.AddReward(killerReward);
        survivor.AddReward(survivorReward);
    }
    
    public bool IsInChase(SurvivorAgent survivor)
    {
        return survivorsInChase.Contains(survivor);
    }
    
    public bool IsKillerInAnyChase()
    {
        return survivorsInChase.Count > 0;
    }
    
    public int GetChaseCount()
    {
        return survivorsInChase.Count;
    }
}
