using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class SurvivorAgent : Agent
{
    [Header("Movement Settings")]
    public float acceleration = 10f;
    public float deceleration = 30f;
    public float maxSpeed = 4f;
    
    [Header("Raycast Settings")]
    public float raycastDistance = 20f;
    public int numCircleRaycasts = 16;
    public LayerMask raycastLayerMask = ~0;
    
    [Header("Episode Settings")]
    public float episodeTimeLimitSeconds = 600f; // 10 minutes
    
    [Header("Scratch Marks")]
    public GameObject scratchMarkPrefab;
    public float scratchMarkSpawnInterval = 1f;
    
    private Rigidbody2D rb;
    private InteractionController interactionController;
    private bool wasInteractPressed = false;
    private float episodeTimer = 0f;
    private float scratchMarkTimer = 0f;
    private bool wasMovingLastFrame = false;
    
    private float storedHorizontal = 0f;
    private float storedVertical = 0f;
    private int storedInteractAction = 0;
    
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[SurvivorAgent] Rigidbody2D component not found!");
        }
        
        interactionController = GetComponent<InteractionController>();
        if (interactionController == null)
        {
            interactionController = gameObject.AddComponent<InteractionController>();
        }
        
        // Ensure the agent requests decisions
        var decisionRequester = GetComponent<Unity.MLAgents.DecisionRequester>();
        if (decisionRequester == null)
        {
            decisionRequester = gameObject.AddComponent<Unity.MLAgents.DecisionRequester>();
            decisionRequester.DecisionPeriod = 1; // Request decision every frame
            decisionRequester.TakeActionsBetweenDecisions = true;
        }
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset survivor state at the start of each episode
        rb.linearVelocity = Vector2.zero;
        episodeTimer = 0f;
        scratchMarkTimer = 0f;
        wasMovingLastFrame = false;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's own position (2 observations)
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        
        // Add agent's own velocity (2 observations)
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.y);
        
        // 16 raycasts in a circle around the survivor
        float angleStep = 360f / numCircleRaycasts;
        
        for (int i = 0; i < numCircleRaycasts; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, raycastDistance, raycastLayerMask);
            
            // Sort hits by distance to process closest first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            // Initialize observation values for this raycast
            float wallDistance = raycastDistance;
            float generatorDistance = raycastDistance;
            float windowDistance = raycastDistance;
            float undroppedPalletDistance = raycastDistance;
            float droppedPalletDistance = raycastDistance;
            float shortWallDistance = raycastDistance;
            
            bool blockedByWall = false;
            
            // Process hits in order of distance
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    float distance = hit.distance;
                    
                    // Check tags and update distances
                    if (hit.collider.CompareTag("Wall"))
                    {
                        wallDistance = Mathf.Min(wallDistance, distance);
                        blockedByWall = true;
                        break; // Wall blocks vision, stop processing this raycast
                    }
                    else if (hit.collider.CompareTag("ShortWall"))
                    {
                        // ShortWall can be seen but doesn't block vision
                        shortWallDistance = Mathf.Min(shortWallDistance, distance);
                    }
                    else if (hit.collider.CompareTag("Generator"))
                    {
                        generatorDistance = Mathf.Min(generatorDistance, distance);
                    }
                    else if (hit.collider.CompareTag("Window"))
                    {
                        windowDistance = Mathf.Min(windowDistance, distance);
                    }
                    else if (hit.collider.CompareTag("UndroppedPallet"))
                    {
                        undroppedPalletDistance = Mathf.Min(undroppedPalletDistance, distance);
                    }
                    else if (hit.collider.CompareTag("DroppedPallet"))
                    {
                        droppedPalletDistance = Mathf.Min(droppedPalletDistance, distance);
                    }
                }
            }
            
            // Add normalized observations (6 per raycast = 96 observations total)
            sensor.AddObservation(wallDistance / raycastDistance);
            sensor.AddObservation(generatorDistance / raycastDistance);
            sensor.AddObservation(windowDistance / raycastDistance);
            sensor.AddObservation(undroppedPalletDistance / raycastDistance);
            sensor.AddObservation(droppedPalletDistance / raycastDistance);
            sensor.AddObservation(shortWallDistance / raycastDistance);
        }
        
        // LOS checks to other survivors (up to 3 others = 12 observations)
        SurvivorAgent[] allSurvivors = FindObjectsByType<SurvivorAgent>(FindObjectsSortMode.None);
        int survivorsChecked = 0;
        
        foreach (SurvivorAgent otherSurvivor in allSurvivors)
        {
            if (otherSurvivor == this || survivorsChecked >= 3)
                continue;
                
            Vector2 directionToSurvivor = otherSurvivor.transform.position - transform.position;
            float distanceToSurvivor = directionToSurvivor.magnitude;
            
            // Raycast to check LOS
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null)
            {
                // Check if we hit the other survivor directly
                if (hit.collider.gameObject == otherSurvivor.gameObject)
                {
                    hasLOS = true;
                }
                // Check if we only hit a ShortWall (can see through it)
                else if (hit.collider.CompareTag("ShortWall"))
                {
                    RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
                    foreach (RaycastHit2D h in allHits)
                    {
                        if (h.collider.gameObject == otherSurvivor.gameObject)
                        {
                            hasLOS = true;
                            break;
                        }
                        else if (h.collider.CompareTag("Wall"))
                        {
                            hasLOS = false;
                            break;
                        }
                    }
                }
            }
            
            // Add observations: hasLOS, relative position x, relative position y, distance
            sensor.AddObservation(hasLOS ? 1f : 0f);
            sensor.AddObservation(hasLOS ? directionToSurvivor.x / raycastDistance : 0f);
            sensor.AddObservation(hasLOS ? directionToSurvivor.y / raycastDistance : 0f);
            sensor.AddObservation(hasLOS ? distanceToSurvivor / raycastDistance : 0f);
            
            survivorsChecked++;
        }
        
        // Fill remaining slots if less than 3 other survivors
        for (int i = survivorsChecked; i < 3; i++)
        {
            sensor.AddObservation(0f); // No LOS
            sensor.AddObservation(0f); // No X
            sensor.AddObservation(0f); // No Y
            sensor.AddObservation(0f); // No distance
        }
        
        // LOS check to killer (4 observations)
        GameObject killer = GameObject.FindGameObjectWithTag("Killer");
        if (killer != null)
        {
            Vector2 directionToKiller = killer.transform.position - transform.position;
            float distanceToKiller = directionToKiller.magnitude;
            
            // Raycast to check LOS
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToKiller.normalized, distanceToKiller, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null)
            {
                // Check if we hit the killer directly
                if (hit.collider.gameObject == killer)
                {
                    hasLOS = true;
                }
                // Check if we only hit a ShortWall (can see through it)
                else if (hit.collider.CompareTag("ShortWall"))
                {
                    RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToKiller.normalized, distanceToKiller, raycastLayerMask);
                    foreach (RaycastHit2D h in allHits)
                    {
                        if (h.collider.gameObject == killer)
                        {
                            hasLOS = true;
                            break;
                        }
                        else if (h.collider.CompareTag("Wall"))
                        {
                            hasLOS = false;
                            break;
                        }
                    }
                }
            }
            
            // Add observations: hasLOS, relative position x, relative position y, distance
            sensor.AddObservation(hasLOS ? 1f : 0f);
            sensor.AddObservation(hasLOS ? directionToKiller.x / raycastDistance : 0f);
            sensor.AddObservation(hasLOS ? directionToKiller.y / raycastDistance : 0f);
            sensor.AddObservation(hasLOS ? distanceToKiller / raycastDistance : 0f);
        }
        else
        {
            // No killer found
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        
        // Total observations: 4 (position + velocity) + 96 (16 raycasts * 6 types) + 12 (3 survivors * 4 values) + 4 (killer) = 116 observations
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Store actions to be applied every frame
        storedHorizontal = actions.ContinuousActions[0];
        storedVertical = actions.ContinuousActions[1];
        storedInteractAction = actions.DiscreteActions[0];
        
        // Generator repair
        if (storedInteractAction == 1 && interactionController.GetCurrentGenerator() != null)
        {
            interactionController.StartGeneratorRepair();
        }
        else
        {
            interactionController.StopGeneratorRepair();
        }
        
        // Small negative reward per step to encourage efficiency
        AddReward(-0.0001f);
        
        // Track episode time and end if time limit reached
        episodeTimer += Time.deltaTime;
        if (episodeTimer >= episodeTimeLimitSeconds)
        {
            AddReward(-0.1f);
            EndEpisode();
        }
    }
    
    void Update()
    {
        // Handle interaction state changes
        if (storedInteractAction == 1 && !wasInteractPressed)
        {
            Debug.Log("[SurvivorAgent] Interact button pressed, calling TryInteract()");
            interactionController.TryInteract();
            wasInteractPressed = true;
        }
        else if (storedInteractAction == 0)
        {
            wasInteractPressed = false;
        }
        
        // Apply movement every frame using stored actions
        ApplyMovement();
        
        // Handle scratch mark spawning
        HandleScratchMarks();
    }
    
    void HandleScratchMarks()
    {
        if (scratchMarkPrefab == null) return;
        
        bool isMoving = rb.linearVelocity.magnitude > 0.1f;
        
        if (isMoving)
        {
            // Reset timer when starting to move
            if (!wasMovingLastFrame)
            {
                scratchMarkTimer = 0f;
            }
            
            scratchMarkTimer += Time.deltaTime;
            
            // Spawn scratch mark every interval
            if (scratchMarkTimer >= scratchMarkSpawnInterval)
            {
                Instantiate(scratchMarkPrefab, transform.position, Quaternion.identity);
                scratchMarkTimer = 0f;
            }
        }
        else
        {
            // Reset timer when stopped
            scratchMarkTimer = 0f;
        }
        
        wasMovingLastFrame = isMoving;
    }
    
    void ApplyMovement()
    {
        // Check if character is locked (pallet throw) or vaulting
        if (InteractionController.IsCharacterLocked(this) || InteractionController.IsCharacterVaulting(this))
        {
            return;
        }
        
        Vector2 input = new Vector2(storedHorizontal, storedVertical);
        
        // Apply deceleration if no input
        if (input.magnitude < 0.01f)
        {
            if (rb.linearVelocity.magnitude > 0.01f)
            {
                Vector2 decelerationVector = -rb.linearVelocity.normalized * deceleration * Time.deltaTime;
                
                if (decelerationVector.magnitude >= rb.linearVelocity.magnitude)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    rb.linearVelocity += decelerationVector;
                }
            }
        }
        else
        {
            // Apply acceleration towards target velocity
            Vector2 inputDirection = input.normalized;
            Vector2 targetVelocity = inputDirection * maxSpeed;
            
            Vector2 velocityChange = targetVelocity - rb.linearVelocity;
            Vector2 accelerationVector = velocityChange.normalized * acceleration * Time.deltaTime;
            
            if (accelerationVector.magnitude > velocityChange.magnitude)
            {
                accelerationVector = velocityChange;
            }
            
            rb.linearVelocity += accelerationVector;
            
            // Clamp velocity to max speed
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
        
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
    
    // Reward methods to be called from other scripts
    public void RewardGeneratorProgress(float progressAmount)
    {
        // Reward for making progress on a generator
        AddReward(progressAmount * 0.1f);
    }
    
    public void RewardPalletStun()
    {
        // Large reward for stunning the killer with a pallet
        AddReward(1.0f);
    }
    
    public void RewardSurvivalInChase(float timeInChase)
    {
        // Small reward for surviving in chase
        AddReward(timeInChase * 0.01f);
    }
    
    public void PenalizeCaught()
    {
        // Large penalty for getting caught by the killer
        AddReward(-2.0f);
        EndEpisode();
    }
    
    public void RewardEscape()
    {
        // Large reward for escaping
        AddReward(5.0f);
        EndEpisode();
    }
    
    public void PenalizeTimeLimit()
    {
        // Large penalty for not escaping within time limit
        AddReward(-5.0f);
        EndEpisode();
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        float angleStep = 360f / numCircleRaycasts;
        
        // Draw circle raycasts (cyan)
        for (int i = 0; i < numCircleRaycasts; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, direction * raycastDistance);
        }
        
        // Draw LOS checks to other survivors
        SurvivorAgent[] allSurvivors = FindObjectsByType<SurvivorAgent>(FindObjectsSortMode.None);
        int survivorsChecked = 0;
        
        foreach (SurvivorAgent otherSurvivor in allSurvivors)
        {
            if (otherSurvivor == this || survivorsChecked >= 3)
                continue;
                
            Vector2 directionToSurvivor = otherSurvivor.transform.position - transform.position;
            float distanceToSurvivor = directionToSurvivor.magnitude;
            
            // Check LOS
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null && hit.collider.gameObject == otherSurvivor.gameObject)
            {
                hasLOS = true;
            }
            else if (hit.collider != null && hit.collider.CompareTag("ShortWall"))
            {
                RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
                foreach (RaycastHit2D h in allHits)
                {
                    if (h.collider.gameObject == otherSurvivor.gameObject)
                    {
                        hasLOS = true;
                        break;
                    }
                    else if (h.collider.CompareTag("Wall"))
                    {
                        break;
                    }
                }
            }
            
            // Draw ray - green if LOS, red if blocked
            Gizmos.color = hasLOS ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, otherSurvivor.transform.position);
            
            survivorsChecked++;
        }
        
        // Draw LOS check to killer (yellow if visible, orange if blocked)
        GameObject killer = GameObject.FindGameObjectWithTag("Killer");
        if (killer != null)
        {
            Vector2 directionToKiller = killer.transform.position - transform.position;
            float distanceToKiller = directionToKiller.magnitude;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToKiller.normalized, distanceToKiller, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null && hit.collider.gameObject == killer)
            {
                hasLOS = true;
            }
            else if (hit.collider != null && hit.collider.CompareTag("ShortWall"))
            {
                RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToKiller.normalized, distanceToKiller, raycastLayerMask);
                foreach (RaycastHit2D h in allHits)
                {
                    if (h.collider.gameObject == killer)
                    {
                        hasLOS = true;
                        break;
                    }
                    else if (h.collider.CompareTag("Wall"))
                    {
                        break;
                    }
                }
            }
            
            // Draw ray - yellow if LOS, orange if blocked
            Gizmos.color = hasLOS ? Color.yellow : new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position, killer.transform.position);
        }
    }
}

