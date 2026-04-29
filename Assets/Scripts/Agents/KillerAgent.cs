using UnityEngine;

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class KillerAgent : Agent
{
    [Header("Movement Settings")]
    public float acceleration = 11.25f;
    public float deceleration = 30f;
    public float maxSpeed = 4.6f;
    
    [Header("Raycast Settings")]
    public float raycastDistance = 20f;
    public int numCircleRaycasts = 16;
    public LayerMask raycastLayerMask = ~0;
    
    [Header("Episode Settings")]
    public float episodeTimeLimitSeconds = 600f; // 10 minutes
    
    [Header("Attack Settings")]
    public float lungeSpeed = 6.9f;
    public float lungeAcceleration = 50f;
    public float lungeChargeMaxTime = 0.5f;
    public float lungeActiveTime = 0.3f;
    public float hitSlowdownSpeed = 0.575f;
    public float hitSlowdownDuration = 2.7f;
    public float missSlowdownSpeed = 1.15f;
    public float missSlowdownDuration = 1.5f;
    public CircleCollider2D attackHitbox; // Assign in inspector
    
    private Rigidbody2D rb;
    private InteractionController interactionController;
    private Transform environmentRoot;
    private MapEnvironmentController environmentController;
    private bool wasInteractPressed = false;
    
    private float storedHorizontal = 0f;
    private float storedVertical = 0f;
    private int storedInteractAction = 0;
    private int storedAttackAction = 0;
    
    private bool isAttacking = false;
    private bool isLunging = false;
    private float lungeChargeTime = 0f;
    private bool wasAttackPressed = false;
    private float attackHoldTime = 0f;
    private Vector2 attackDirection = Vector2.zero;
    
    private ChaseManager chaseManager;
    
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[KillerAgent] Rigidbody2D component not found!");
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
            Debug.LogError("[KillerAgent] No parent found! Agent must be child of a Map.");
        }
        else
        {
            // Get the MapEnvironmentController
            environmentController = environmentRoot.GetComponent<MapEnvironmentController>();
            if (environmentController == null)
            {
                Debug.LogWarning("[KillerAgent] MapEnvironmentController not found on parent. Adding one.");
                environmentController = environmentRoot.gameObject.AddComponent<MapEnvironmentController>();
            }
            
            // Find or add ChaseManager
            chaseManager = GetComponent<ChaseManager>();
            if (chaseManager == null)
            {
                chaseManager = gameObject.AddComponent<ChaseManager>();
            }
        }
        
        // Find attack hitbox if not assigned
        if (attackHitbox == null)
        {
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    attackHitbox = col;
                    break;
                }
            }
            if (attackHitbox == null)
            {
                Debug.LogWarning("[KillerAgent] Attack hitbox not found! Add a trigger CircleCollider2D.");
            }
        }
        
        // Ensure the agent requests decisions
        var decisionRequester = GetComponent<Unity.MLAgents.DecisionRequester>();
        if (decisionRequester == null)
        {
            decisionRequester = gameObject.AddComponent<Unity.MLAgents.DecisionRequester>();
            decisionRequester.DecisionPeriod = 1; // Request decision every frame or everything breaks
            decisionRequester.TakeActionsBetweenDecisions = true;
        }
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset killer state at the start of each episode
        rb.linearVelocity = Vector2.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's own position (2 observations)
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        
        // Add agent's own velocity (2 observations)
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.y);
        
        // 16 raycasts in a circle around the killer
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
            float scratchMarksDistance = raycastDistance;
            
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
                    else if (hit.collider.CompareTag("ScratchMarks"))
                    {
                        // ScratchMarks can be seen but don't block vision
                        scratchMarksDistance = Mathf.Min(scratchMarksDistance, distance);
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
            
            // Add normalized observations (7 per raycast = 112 observations total)
            sensor.AddObservation(wallDistance / raycastDistance);
            sensor.AddObservation(generatorDistance / raycastDistance);
            sensor.AddObservation(windowDistance / raycastDistance);
            sensor.AddObservation(undroppedPalletDistance / raycastDistance);
            sensor.AddObservation(droppedPalletDistance / raycastDistance);
            sensor.AddObservation(shortWallDistance / raycastDistance);
            sensor.AddObservation(scratchMarksDistance / raycastDistance);
        }
        
        // LOS checks to all survivors (up to 4 survivors = 16 observations)
        SurvivorAgent[] allSurvivors = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
        int survivorsChecked = 0;
        
        foreach (SurvivorAgent survivor in allSurvivors)
        {
            if (survivorsChecked >= 4)
                break;
                
            Vector2 directionToSurvivor = survivor.transform.position - transform.position;
            float distanceToSurvivor = directionToSurvivor.magnitude;
            
            // Raycast to check LOS
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null)
            {
                // Check if we hit the survivor directly
                if (hit.collider.gameObject == survivor.gameObject)
                {
                    hasLOS = true;
                }
                // Check if we only hit a ShortWall (can see through it)
                else if (hit.collider.CompareTag("ShortWall"))
                {
                    RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
                    foreach (RaycastHit2D h in allHits)
                    {
                        if (h.collider.gameObject == survivor.gameObject)
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
        
        // Fill remaining slots if less than 4 survivors, which there should never be but just in case
        for (int i = survivorsChecked; i < 4; i++)
        {
            sensor.AddObservation(0f); // No LOS
            sensor.AddObservation(0f); // No X
            sensor.AddObservation(0f); // No Y
            sensor.AddObservation(0f); // No distance
        }
        
        // Health state observations for all survivors (4 survivors = 4 observations)
        SurvivorAgent[] allSurvivorsForHealth = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
        int healthStateIndex = 0;
        
        foreach (SurvivorAgent survivor in allSurvivorsForHealth)
        {
            if (healthStateIndex >= 4) break;
            
            sensor.AddObservation((float)survivor.GetHealthState() / 2f); // Normalize: 0=Healthy, 0.5=Injured, 1=Dying
            healthStateIndex++;
        }
        
        // Fill remaining slots if less than 4 survivors
        for (int i = healthStateIndex; i < 4; i++)
        {
            sensor.AddObservation(0f); // Default to healthy
        }
        
        // In chase observation (1 observation)
        bool inChase = chaseManager != null && chaseManager.IsKillerInAnyChase();
        sensor.AddObservation(inChase ? 1f : 0f);
        
        // Total observations: 4 (position + velocity) + 112 (16 raycasts * 7 types) + 16 (4 survivors * 4 values) + 4 (survivor health states) + 1 (in chase) = 137 observations
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Store actions to be applied every frame
        storedHorizontal = actions.ContinuousActions[0];
        storedVertical = actions.ContinuousActions[1];
        storedInteractAction = actions.DiscreteActions[0];
        storedAttackAction = actions.DiscreteActions[1]; // Attack action
        
        // Small negative reward per step to encourage efficiency
        AddReward(-0.0001f);
        
    }
    
    void Update()
    {
        // Handle interaction state changes
        if (storedInteractAction == 1 && !wasInteractPressed)
        {
            interactionController.TryInteract();
            wasInteractPressed = true;
        }
        else if (storedInteractAction == 0)
        {
            wasInteractPressed = false;
        }
        
        // Handle attack state changes
        if (!isAttacking)
        {
            if (storedAttackAction == 1)
            {
                if (!wasAttackPressed)
                {
                    // First frame of attack press
                    wasAttackPressed = true;
                    attackHoldTime = 0f;
                }
                else
                {
                    // Holding attack - track time
                    attackHoldTime += Time.deltaTime;
                    
                    // If held long enough, start lunge
                    if (attackHoldTime >= Time.deltaTime * 3 && !isLunging)
                    {
                        StartAttack(true);
                    }
                }
            }
            else if (storedAttackAction == 0 && wasAttackPressed)
            {
                // Released attack button
                if (attackHoldTime < Time.deltaTime * 3)
                {
                    // Quick tap - normal attack
                    StartAttack(false);
                }
                wasAttackPressed = false;
                attackHoldTime = 0f;
            }
        }
        
        ApplyMovement();
    }
    
    void StartAttack(bool isLunge)
    {
        // Store the direction the killer is moving
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            attackDirection = rb.linearVelocity.normalized;
        }
        else
        {
            Vector2 input = new Vector2(storedHorizontal, storedVertical);
            attackDirection = input.magnitude > 0.1f ? input.normalized : Vector2.down;
        }
        
        if (isLunge)
        {
            // Start lunge
            StartCoroutine(PerformLungeAttack());
        }
        else
        {
            // Normal attack (quick tap)
            StartCoroutine(PerformNormalAttack());
        }
    }
    
    System.Collections.IEnumerator PerformNormalAttack()
    {
        isAttacking = true;
        
        // Check for hits immediately
        bool hitSurvivor = CheckForHit();
        
        if (hitSurvivor)
        {
            // Hit: slow to 0.575 m/s for 2.7s
            yield return StartCoroutine(ApplyAttackSlowdown(hitSlowdownSpeed, hitSlowdownDuration));
        }
        else
        {
            // Miss: slow to 1.15 m/s for 1.5s and penalize
            AddReward(-0.1f); // Penalty for missing
            yield return StartCoroutine(ApplyAttackSlowdown(missSlowdownSpeed, missSlowdownDuration));
        }
        
        isAttacking = false;
    }
    
    System.Collections.IEnumerator PerformLungeAttack()
    {
        isAttacking = true;
        isLunging = true;
        
        // Phase 1: Lunge movement (up to 0.5s or until button released)
        float lungeTime = 0f;
        
        while (lungeTime < lungeChargeMaxTime && storedAttackAction == 1)
        {
            // Get current input for direction (allow turning during lunge)
            Vector2 input = new Vector2(storedHorizontal, storedVertical);
            
            if (input.magnitude > 0.1f)
            {
                // Accelerate toward lunge speed in input direction using acceleration
                Vector2 inputDirection = input.normalized;
                Vector2 targetVelocity = inputDirection * lungeSpeed;
                
                Vector2 velocityChange = targetVelocity - rb.linearVelocity;
                float accel = rb.linearVelocity.magnitude < lungeSpeed ? lungeAcceleration : acceleration;
                Vector2 accelerationVector = velocityChange.normalized * accel * Time.deltaTime;
                
                if (accelerationVector.magnitude > velocityChange.magnitude)
                {
                    accelerationVector = velocityChange;
                }
                
                rb.linearVelocity += accelerationVector;
                
                // Only clamp if over lunge speed
                if (rb.linearVelocity.magnitude > lungeSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * lungeSpeed;
                }
                
                attackDirection = inputDirection;
            }
            
            lungeTime += Time.deltaTime;
            yield return null;
        }
        
        // Check for hit immediately after lunge
        bool hitSurvivor = CheckForHit();
        
        // Attack cooldown with slowdown
        if (hitSurvivor)
        {
            yield return StartCoroutine(ApplyAttackSlowdown(hitSlowdownSpeed, hitSlowdownDuration));
        }
        else
        {
            // Miss: penalize and slow down
            AddReward(-0.1f); // Penalty for missing
            yield return StartCoroutine(ApplyAttackSlowdown(missSlowdownSpeed, missSlowdownDuration));
        }
        
        isAttacking = false;
        isLunging = false;
    }
    
    System.Collections.IEnumerator ApplyAttackSlowdown(float targetSpeed, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // x^3 slope in/out (ease in and out)
            float smoothT = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            
            float currentMaxSpeed = Mathf.Lerp(targetSpeed, maxSpeed, smoothT);
            
            Vector2 input = new Vector2(storedHorizontal, storedVertical);
            
            if (input.magnitude > 0.1f)
            {
                // Apply acceleration towards input direction, clamped to slowdown speed
                Vector2 inputDirection = input.normalized;
                Vector2 targetVelocity = inputDirection * currentMaxSpeed;
                
                Vector2 velocityChange = targetVelocity - rb.linearVelocity;
                Vector2 accelerationVector = velocityChange.normalized * acceleration * Time.deltaTime;
                
                if (accelerationVector.magnitude > velocityChange.magnitude)
                {
                    accelerationVector = velocityChange;
                }
                
                rb.linearVelocity += accelerationVector;
                
                // Only clamp if over max
                if (rb.linearVelocity.magnitude > currentMaxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
                }
            }
            else
            {
                // No input
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
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    bool CheckForHit()
    {
        if (attackHitbox == null) return false;
        
        // Get all colliders in the attack hitbox
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.transform.position, attackHitbox.radius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Survivor"))
            {
                // Hit a survivor
                SurvivorAgent survivorAgent = hit.GetComponent<SurvivorAgent>();
                if (survivorAgent != null)
                {
                    SurvivorHealthState currentState = survivorAgent.GetHealthState();
                    
                    // Ignore dying survivors
                    if (currentState == SurvivorHealthState.Dying)
                    {
                        continue;
                    }
                    
                    if (currentState == SurvivorHealthState.Healthy)
                    {
                        // Healthy -> Injured
                        survivorAgent.SetHealthState(SurvivorHealthState.Injured);
                        RewardHitSurvivor();
                        return true; // Successfully hit one survivor
                    }
                    else if (currentState == SurvivorHealthState.Injured)
                    {
                        // Injured -> Dying (downed)
                        survivorAgent.SetHealthState(SurvivorHealthState.Dying);
                        RewardCatchSurvivor();
                        return true; // Successfully hit one survivor
                    }
                }
            }
        }
        
        return false;
    }
    
    public System.Collections.IEnumerator PerformGeneratorKick(Generator generator)
    {
        // Lock the killer for 2.34 seconds
        InteractionController.LockCharacter(this);
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        
        // Wait for kick duration
        yield return new UnityEngine.WaitForSeconds(2.34f);
        
        // After kick completes, start generator regression
        if (generator != null)
        {
            generator.StartRegression();
        }
        
        InteractionController.UnlockCharacter(this);
        
        RewardDamageGenerator();
    }
    
    public System.Collections.IEnumerator PerformPalletBreak(PalletController pallet, Transform breakPosition)
    {
        // Lock the killer
        InteractionController.LockCharacter(this);
        
        // Move killer to break position
        if (breakPosition != null)
        {
            transform.position = breakPosition.position;
        }
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        
        // Wait for break duration (2.34 seconds)
        yield return new UnityEngine.WaitForSeconds(2.34f);
        
        // Destroy the pallet
        if (pallet != null)
        {
            pallet.DestroyPallet();
        }
        
        // Unlock the killer
        InteractionController.UnlockCharacter(this);
        
        RewardBreakPallet();
    }
    
    void ApplyMovement()
    {
        // Check if character is locked, vaulting, or attacking
        if (InteractionController.IsCharacterLocked(this) || InteractionController.IsCharacterVaulting(this) || isAttacking)
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
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Interact
        discreteActions[1] = Input.GetMouseButton(0) ? 1 : 0; // Attack (left click)
    }
    
    // Reward methods to be called from other scripts
    public void RewardCatchSurvivor()
    {
        // Large reward for catching a survivor (increased from 2.0f to 3.0f in v2)
        AddReward(3.0f);
    }
    
    public void RewardHitSurvivor()
    {
        // Reward for hitting a survivor (increased from 0.5f to 1.0f in v2)
        AddReward(1.0f);
    }
    
    public void RewardDamageGenerator()
    {
        // Reward for damaging/regressing a generator
        AddReward(0.3f);
    }
    
    public void RewardBreakPallet()
    {
        // Reward for breaking a pallet
        AddReward(0.2f);
    }
    
    public void PenalizeGeneratorProgress(float progressAmount)
    {
        // Penalty for survivors making generator progress
        // Scale penalty based on how much progress was made
        AddReward(-progressAmount * 0.01f);
    }
    
    public void RewardPalletStunned()
    {
        // Penalty for getting stunned by a pallet
        AddReward(-0.5f);
    }
    
    public void RewardAllSurvivorsCaught()
    {
        // Large reward for catching all survivors
        AddReward(10.0f);
    }
    
    public void RewardAllSurvivorsEliminated()
    {
        // Large reward for eliminating all survivors (third down)
        AddReward(10.0f);
    }
    
    public void RewardAllSurvivorsDown()
    {
        // Large reward for getting all survivors into dying state simultaneously
        AddReward(10.0f);
    }
    
    public void PenalizeGeneratorsCompleted()
    {
        // Penalty for survivors completing enough generators
        AddReward(-10.0f);
    }
    
    public void PenalizeTimeLimit()
    {
        // Penalty for exceeding time limit
        AddReward(-20.0f);
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        float angleStep = 360f / numCircleRaycasts;
        
        // Draw circle raycasts
        for (int i = 0; i < numCircleRaycasts; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, direction * raycastDistance);
        }
        
        // Draw LOS checks to all survivors
        SurvivorAgent[] allSurvivors = environmentRoot != null ? 
            environmentRoot.GetComponentsInChildren<SurvivorAgent>() : 
            new SurvivorAgent[0];
        int survivorsChecked = 0;
        
        foreach (SurvivorAgent survivor in allSurvivors)
        {
            if (survivorsChecked >= 4)
                break;
                
            Vector2 directionToSurvivor = survivor.transform.position - transform.position;
            float distanceToSurvivor = directionToSurvivor.magnitude;
            
            // Check LOS
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
            
            bool hasLOS = false;
            if (hit.collider != null && hit.collider.gameObject == survivor.gameObject)
            {
                hasLOS = true;
            }
            else if (hit.collider != null && hit.collider.CompareTag("ShortWall"))
            {
                RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, directionToSurvivor.normalized, distanceToSurvivor, raycastLayerMask);
                foreach (RaycastHit2D h in allHits)
                {
                    if (h.collider.gameObject == survivor.gameObject)
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
            
            // for editor
            Gizmos.color = hasLOS ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position, survivor.transform.position);
            
            survivorsChecked++;
        }
    }
}

