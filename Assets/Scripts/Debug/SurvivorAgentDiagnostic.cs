using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Attach this to your SurvivorAgent GameObject to diagnose common issues
/// Check the Console for diagnostic output when you press Play
/// </summary>
public class SurvivorAgentDiagnostic : MonoBehaviour
{
    private SurvivorAgent agent;
    
    void Start()
    {
        Debug.Log("=== SURVIVOR AGENT DIAGNOSTIC ===");
        
        // Check for required components
        agent = GetComponent<SurvivorAgent>();
        if (agent == null)
        {
            Debug.LogError("? SurvivorAgent component NOT FOUND!");
        }
        else
        {
            Debug.Log("? SurvivorAgent component found");
        }
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("? Rigidbody2D component NOT FOUND! Add one!");
        }
        else
        {
            Debug.Log($"? Rigidbody2D found - BodyType: {rb.bodyType}, Simulated: {rb.simulated}");
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogWarning("?? Rigidbody2D should be Dynamic!");
            }
            if (!rb.simulated)
            {
                Debug.LogWarning("?? Rigidbody2D Simulated should be checked!");
            }
        }
        
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("?? No Collider2D found - agent won't collide with anything");
        }
        else
        {
            Debug.Log($"? Collider2D found: {collider.GetType().Name}");
        }
        
        var behaviorParams = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (behaviorParams == null)
        {
            Debug.LogError("? BehaviorParameters component NOT FOUND! Add one!");
        }
        else
        {
            Debug.Log($"? BehaviorParameters found");
            Debug.Log($"   - Behavior Name: {behaviorParams.BehaviorName}");
            Debug.Log($"   - Behavior Type: {behaviorParams.BehaviorType}");
            Debug.Log($"   - Vector Observation Size: {behaviorParams.BrainParameters.VectorObservationSize}");
            Debug.Log($"   - Action Spec: {behaviorParams.BrainParameters.ActionSpec}");
            
            if (behaviorParams.BehaviorType != Unity.MLAgents.Policies.BehaviorType.HeuristicOnly)
            {
                Debug.LogError($"? Behavior Type is {behaviorParams.BehaviorType} - should be HeuristicOnly for manual control!");
            }
            
            if (behaviorParams.BrainParameters.VectorObservationSize != 116)
            {
                Debug.LogWarning($"?? Vector Observation Size is {behaviorParams.BrainParameters.VectorObservationSize}, expected 116");
            }
            
            var actionSpec = behaviorParams.BrainParameters.ActionSpec;
            if (actionSpec.NumContinuousActions != 2)
            {
                Debug.LogWarning($"?? Continuous Actions is {actionSpec.NumContinuousActions}, expected 2");
            }
        }
        
        // Check tag
        if (!CompareTag("Survivor"))
        {
            Debug.LogWarning($"?? GameObject tag is '{tag}', should be 'Survivor'");
        }
        else
        {
            Debug.Log("? GameObject tagged as 'Survivor'");
        }
        
        Debug.Log("=== END DIAGNOSTIC ===");
        Debug.Log("Press WASD to test movement. Watch for stored action values below.");
    }
    
    void Update()
    {
        if (agent == null) return;
        
        // Access the stored values using reflection
        var type = typeof(SurvivorAgent);
        var horizontalField = type.GetField("storedHorizontal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var verticalField = type.GetField("storedVertical", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var interactField = type.GetField("storedInteractAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (horizontalField != null && verticalField != null && interactField != null)
        {
            float h = (float)horizontalField.GetValue(agent);
            float v = (float)verticalField.GetValue(agent);
            int interact = (int)interactField.GetValue(agent);
            
            bool spacePressed = Input.GetKey(KeyCode.Space);
            
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f || spacePressed)
            {
                var rb = GetComponent<Rigidbody2D>();
                Debug.Log($"[Agent State] StoredH:{h:F2} StoredV:{v:F2} StoredInteract:{interact} | Velocity:{rb.linearVelocity} | Input H:{Input.GetAxisRaw("Horizontal"):F2} V:{Input.GetAxisRaw("Vertical"):F2} Space:{spacePressed}");
            }
        }
    }
}
