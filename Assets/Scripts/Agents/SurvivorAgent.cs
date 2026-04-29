using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public enum SurvivorHealthState
{
    Healthy = 0,
    Injured = 1,
    Dying = 2
}

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
    
    [Header("Health State")]
    public SurvivorHealthState healthState = SurvivorHealthState.Healthy;
    
    [Header("Speed Boost Settings")]
    public float speedBoostMaxSpeed = 6.6f;
    public float speedBoostDuration = 1.8f;
    
    [Header("Dying State Settings")]
    public float dyingStateMaxSpeed = 0.7f;
    
    [Header("Elimination Settings")]
    public int downCount = 0;
    public int maxDownsBeforeElimination = 3;
    public bool isEliminated = false;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private InteractionController interactionController;
    private Transform environmentRoot;
    private MapEnvironmentController environmentController;
    private bool wasInteractPressed = false;
    private float scratchMarkTimer = 0f;
    private bool wasMovingLastFrame = false;
    
    private bool hasSpeedBoost = false;
    private float speedBoostTimer = 0f;
    
    private float storedHorizontal = 0f;
    private float storedVertical = 0f;
    private int storedInteractAction = 0;
    
    private ChaseManager chaseManager;
    
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[SurvivorAgent] Rigidbody2D component not found!");
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[SurvivorAgent] SpriteRenderer component not found!");
        }
        
        interactionController = GetComponent<InteractionController>();
        if (interactionController == null)
        {
            interactionController = gameObject.AddComponent<InteractionController>();
        }
        
        // Find the environment root (Map_X parent)
        environmentRoot = transform.parent;
        if (environmentRoot == null)
        {
            Debug.LogError("[SurvivorAgent] No parent found! Agent must be child of a Map.");
        }
        else
        {
            // Get the MapEnvironmentController
            environmentController = environmentRoot.GetComponent<MapEnvironmentController>();
            if (environmentController == null)
            {
                Debug.LogWarning("[SurvivorAgent] MapEnvironmentController not found on parent. Adding one.");
                environmentController = environmentRoot.gameObject.AddComponent<MapEnvironmentController>();
            }
            
            // Find or add ChaseManager on the killer
            KillerAgent killerAgent = environmentRoot.GetComponentInChildren<KillerAgent>();
            if (killerAgent != null)
            {
                chaseManager = killerAgent.GetComponent<ChaseManager>();
                if (chaseManager == null)
                {
                    chaseManager = killerAgent.gameObject.AddComponent<ChaseManager>();
                }
            }
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
        scratchMarkTimer = 0f;
        wasMovingLastFrame = false;
        healthState = SurvivorHealthState.Healthy;
        hasSpeedBoost = false;
        speedBoostTimer = 0f;
        downCount = 0;
        isEliminated = false;
        
        // Make sure the survivor is visible and active
        gameObject.SetActive(true);
        
        UpdateHealthColor();
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
        SurvivorAgent[] allSurvivors = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
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
        
        // Health state observations for all survivors (4 survivors = 4 observations)
        // Find all survivors and observe their health states
        SurvivorAgent[] allSurvivorsForHealth = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
        bool[] healthStateAdded = new bool[4];
        int healthStateIndex = 0;
        
        // First add this survivor's health state
        sensor.AddObservation((float)healthState / 2f); // Normalize: 0=Healthy, 0.5=Injured, 1=Dying
        healthStateAdded[healthStateIndex++] = true;
        
        // Then add other survivors' health states
        foreach (SurvivorAgent survivor in allSurvivorsForHealth)
        {
            if (healthStateIndex >= 4) break;
            
            if (survivor != this)
            {
                sensor.AddObservation((float)survivor.healthState / 2f);
                healthStateAdded[healthStateIndex++] = true;
            }
        }
        
        // Fill remaining slots if less than 4 survivors
        for (int i = healthStateIndex; i < 4; i++)
        {
            sensor.AddObservation(0f); // Default to healthy
        }
        
        // LOS check to killer (4 observations)
        KillerAgent killerAgent = environmentRoot != null ? 
            environmentRoot.GetComponentInChildren<KillerAgent>() : 
            null;
        GameObject killer = killerAgent != null ? killerAgent.gameObject : null;
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
        
        // Current interaction progress (1 observation)
        float interactionProgress = 0f;
        
        // Check if healing another survivor
        SurvivorHealing healTarget = interactionController.GetCurrentHealTarget();
        if (healTarget != null)
        {
            interactionProgress = healTarget.healingProgress / healTarget.maxHealingProgress;
        }
        // Check if repairing generator
        else
        {
            Generator currentGenerator = interactionController.GetCurrentGenerator();
            if (currentGenerator != null)
            {
                interactionProgress = currentGenerator.progress / currentGenerator.maxProgress;
            }
        }
        
        sensor.AddObservation(interactionProgress);
        
        // In chase observation (1 observation)
        bool inChase = chaseManager != null && chaseManager.IsInChase(this);
        sensor.AddObservation(inChase ? 1f : 0f);
        
        // Total observations: 4 (position + velocity) + 96 (16 raycasts * 6 types) + 12 (3 survivors * 4 values) + 4 (all survivor health states) + 4 (killer) + 1 (interaction progress) + 1 (in chase) = 122 observations
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Store actions to be applied every frame
        storedHorizontal = actions.ContinuousActions[0];
        storedVertical = actions.ContinuousActions[1];
        storedInteractAction = actions.DiscreteActions[0];
        
        // Dying survivors cannot interact
        if (healthState == SurvivorHealthState.Dying)
        {
            storedInteractAction = 0;
        }
        
        // Handle continuous interactions (healing and generator repair)
        if (storedInteractAction == 1)
        {
            // Check if healing another survivor (prioritize healing)
            if (interactionController.GetCurrentHealTarget() != null)
            {
                SurvivorHealing healTarget = interactionController.GetCurrentHealTarget();
                if (healTarget.CanBeHealed() && healthState != SurvivorHealthState.Dying)
                {
                    interactionController.StartHealing();
                    // Don't stop generator repair here, only when actively healing
                    if (interactionController.IsHealing())
                    {
                        interactionController.StopGeneratorRepair();
                    }
                }
                else
                {
                    interactionController.StopHealing();
                    // Try generator repair if can't heal
                    if (interactionController.GetCurrentGenerator() != null)
                    {
                        interactionController.StartGeneratorRepair();
                    }
                }
            }
            // Generator repair (if not healing)
            else if (interactionController.GetCurrentGenerator() != null)
            {
                interactionController.StartGeneratorRepair();
            }
        }
        else
        {
            // Stop both healing and repairing when not interacting
            interactionController.StopHealing();
            interactionController.StopGeneratorRepair();
        }
        
        // Small negative reward per step to encourage efficiency
        AddReward(-0.0001f);
        
        // Episode time is now tracked by MapEnvironmentController
        // No need to track it here or call PenalizeTimeLimit
    }
    
    void Update()
    {
        // Handle interaction state changes (dying survivors cannot interact)
        if (storedInteractAction == 1 && !wasInteractPressed && healthState != SurvivorHealthState.Dying)
        {
            // Only call TryInteract for one-time interactions (vaults, pallets)
            // Don't call it if we're in range of a heal target or generator (continuous interactions)
            bool isContinuousInteraction = interactionController.GetCurrentHealTarget() != null || 
                                          interactionController.GetCurrentGenerator() != null;
            
            if (!isContinuousInteraction)
            {
                Debug.Log("[SurvivorAgent] Interact button pressed, calling TryInteract()");
                interactionController.TryInteract();
            }
            
            wasInteractPressed = true;
        }
        else if (storedInteractAction == 0)
        {
            wasInteractPressed = false;
        }
        
        // Handle speed boost timer
        if (hasSpeedBoost)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                hasSpeedBoost = false;
                speedBoostTimer = 0f;
            }
        }
        
        // Apply movement every frame using stored actions
        ApplyMovement();
        
        // Handle scratch mark spawning
        HandleScratchMarks();
    }
    
    void HandleScratchMarks()
    {
        if (scratchMarkPrefab == null) return;
        
        // Dying survivors don't leave scratch marks
        if (healthState == SurvivorHealthState.Dying)
        {
            scratchMarkTimer = 0f;
            wasMovingLastFrame = false;
            return;
        }
        
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
        
        // Determine current max speed based on health state
        float currentMaxSpeed;
        if (healthState == SurvivorHealthState.Dying)
        {
            currentMaxSpeed = dyingStateMaxSpeed;
        }
        else if (hasSpeedBoost)
        {
            currentMaxSpeed = speedBoostMaxSpeed;
        }
        else
        {
            currentMaxSpeed = maxSpeed;
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
            Vector2 targetVelocity = inputDirection * currentMaxSpeed;
            
            Vector2 velocityChange = targetVelocity - rb.linearVelocity;
            Vector2 accelerationVector = velocityChange.normalized * acceleration * Time.deltaTime;
            
            if (accelerationVector.magnitude > velocityChange.magnitude)
            {
                accelerationVector = velocityChange;
            }
            
            rb.linearVelocity += accelerationVector;
            
            // Clamp velocity to current max speed
            if (rb.linearVelocity.magnitude > currentMaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
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
    public void RewardStartGenerator()
    {
        // Large reward for starting to repair a generator (first time only)
        AddReward(0.5f);
    }
    
    public void RewardGeneratorProgress(float progressAmount)
    {
        // Reward for making progress on a generator (increased from 0.1f to 0.2f)
        AddReward(progressAmount * 0.2f);
    }
    
    public void RewardHealingProgress(float progressAmount)
    {
        // Reward for making progress on healing another survivor
        AddReward(progressAmount * 0.05f);
    }
    
    public void RewardCompletedHeal()
    {
        // Reward for successfully healing another survivor to full health
        AddReward(0.5f);
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
        // Don't end episode here - let environment controller handle it
    }
    
    public void RewardEscape()
    {
        // Large reward for escaping
        AddReward(5.0f);
        
        // Notify environment controller
        if (environmentController != null)
        {
            environmentController.OnSurvivorsEscape();
        }
    }
    
    public void RewardGeneratorsCompleted()
    {
        // Reward for completing enough generators to escape
        AddReward(3.0f);
    }
    
    public void PenalizeTimeLimit()
    {
        // This is now handled by MapEnvironmentController
        // Keep the method for backward compatibility but don't call EndEpisode
        AddReward(-20.0f);
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
        SurvivorAgent[] allSurvivors = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
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
        KillerAgent killerAgent = environmentRoot != null ? 
            environmentRoot.GetComponentInChildren<KillerAgent>() : 
            null;
        GameObject killer = killerAgent != null ? killerAgent.gameObject : null;
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
    
    public void SetHealthState(SurvivorHealthState newState)
    {
        SurvivorHealthState previousState = healthState;
        healthState = newState;
        UpdateHealthColor();
        
        // Handle rewards/penalties based on health state changes
        if (previousState != newState)
        {
            // Penalize losing health states
            if (previousState == SurvivorHealthState.Healthy && newState == SurvivorHealthState.Injured)
            {
                AddReward(-0.5f); // Penalty for being injured
                ApplySpeedBoost();
            }
            else if (previousState == SurvivorHealthState.Injured && newState == SurvivorHealthState.Dying)
            {
                AddReward(-1.0f); // Larger penalty for being downed
                downCount++;
                
                // Check if all survivors are now dying (impossible to recover)
                CheckAllSurvivorsDying();
                
                // Check if this is the third down (elimination)
                if (downCount >= maxDownsBeforeElimination)
                {
                    EliminateSurvivor();
                }
            }
            
            // Reward gaining health states (being healed)
            else if (previousState == SurvivorHealthState.Dying && newState == SurvivorHealthState.Injured)
            {
                AddReward(1.0f); // Reward for being picked up from dying
            }
            else if (previousState == SurvivorHealthState.Injured && newState == SurvivorHealthState.Healthy)
            {
                AddReward(0.5f); // Reward for being fully healed
            }
        }
    }
    
    private void EliminateSurvivor()
    {
        // Heavy penalty for being eliminated
        AddReward(-5.0f);
        
        isEliminated = true;
        
        // Hide the survivor (or you could destroy it, but keeping it helps with training)
        gameObject.SetActive(false);
        
        Debug.Log($"[SurvivorAgent] {gameObject.name} has been eliminated (down #{downCount})");
        
        // Check if all remaining survivors are dying (edge case: if someone was eliminated while others are dying)
        CheckAllSurvivorsDying();
        
        // Check if all survivors are eliminated
        CheckAllSurvivorsEliminated();
    }
    
    private void CheckAllSurvivorsDying()
    {
        if (environmentRoot == null)
        {
            Debug.LogWarning("[SurvivorAgent] CheckAllSurvivorsDying: environmentRoot is null");
            return;
        }
        
        SurvivorAgent[] allSurvivors = environmentRoot.GetComponentsInChildren<SurvivorAgent>(true);
        
        bool allDying = true;
        int survivorCount = 0;
        int dyingCount = 0;
        int eliminatedCount = 0;
        
        foreach (SurvivorAgent survivor in allSurvivors)
        {
            if (!survivor.isEliminated)
            {
                survivorCount++;
                SurvivorHealthState state = survivor.GetHealthState();
                
                if (state == SurvivorHealthState.Dying)
                {
                    dyingCount++;
                }
                else
                {
                    allDying = false;
                }
            }
            else
            {
                eliminatedCount++;
            }
        }
        
        Debug.Log($"[SurvivorAgent] CheckAllSurvivorsDying: Total={allSurvivors.Length}, Active={survivorCount}, Dying={dyingCount}, Eliminated={eliminatedCount}, AllDying={allDying}");
        
        // Only trigger if we actually have survivors and they're all dying
        if (allDying && survivorCount > 0)
        {
            Debug.Log($"[SurvivorAgent] All {survivorCount} active survivors are dying! Triggering episode end.");
            
            // All survivors are dying - notify environment controller
            if (environmentController != null)
            {
                environmentController.OnAllSurvivorsDying();
            }
            else
            {
                Debug.LogError("[SurvivorAgent] environmentController is NULL! Cannot end episode!");
            }
        }
        else
        {
            Debug.Log($"[SurvivorAgent] Not all survivors dying yet. Active={survivorCount}, Dying={dyingCount}");
        }
    }
    
    private void CheckAllSurvivorsEliminated()
    {
        if (environmentRoot == null) return;
        
        SurvivorAgent[] allSurvivors = environmentRoot.GetComponentsInChildren<SurvivorAgent>(true); // Include inactive
        
        bool allEliminated = true;
        foreach (SurvivorAgent survivor in allSurvivors)
        {
            if (!survivor.isEliminated)
            {
                allEliminated = false;
                break;
            }
        }
        
        if (allEliminated)
        {
            // All survivors eliminated - notify environment controller
            if (environmentController != null)
            {
                environmentController.OnAllSurvivorsEliminated();
            }
        }
    }
    
    private void ApplySpeedBoost()
    {
        hasSpeedBoost = true;
        speedBoostTimer = speedBoostDuration;
    }
    
    public SurvivorHealthState GetHealthState()
    {
        return healthState;
    }
    
    private void UpdateHealthColor()
    {
        if (spriteRenderer == null) return;
        
        switch (healthState)
        {
            case SurvivorHealthState.Healthy:
                spriteRenderer.color = new Color(0f, 0.807f, 0.819f); // #00CED1
                break;
            case SurvivorHealthState.Injured:
                spriteRenderer.color = new Color(0f, 0.412f, 0.819f); // #0069D1
                break;
            case SurvivorHealthState.Dying:
                spriteRenderer.color = new Color(0.161f, 0f, 0.819f); // #2900D1
                break;
        }
    }
}

