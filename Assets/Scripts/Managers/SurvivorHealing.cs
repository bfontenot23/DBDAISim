using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SurvivorHealing : MonoBehaviour
{
    [Header("Healing Progress")]
    public float healingProgress = 0f;
    public float maxHealingProgress = 16f;
    public float baseHealingSpeed = 1f;
    private List<GameObject> healingSurvivors = new List<GameObject>();
    private HashSet<MonoBehaviour> survivorsInRange = new HashSet<MonoBehaviour>();
    
    [Header("UI")]
    public Slider progressBar; // Make public so it can be assigned in inspector
    private GameObject progressCanvas;
    private Transform uiTransform;
    
    private SurvivorAgent survivorAgent;
    
    void Awake()
    {
        // Find and hide progress canvas immediately
        if (progressBar == null)
        {
            progressBar = GetComponentInChildren<Slider>();
        }
        
        if (progressBar != null)
        {
            progressCanvas = progressBar.GetComponentInParent<Canvas>().gameObject;
            if (progressCanvas != null)
            {
                progressCanvas.SetActive(false);
            }
        }
    }
    
    void Start()
    {
        survivorAgent = GetComponent<SurvivorAgent>();
        if (survivorAgent == null)
        {
            Debug.LogError("[SurvivorHealing] SurvivorAgent component not found!");
        }
        
        if (progressBar == null)
        {
            progressBar = GetComponentInChildren<Slider>();
        }
        
        if (progressBar != null)
        {
            // Get the canvas parent
            Canvas canvas = progressBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                progressCanvas = canvas.gameObject;
                progressCanvas.SetActive(false);
                uiTransform = progressCanvas.transform;
            }
            
            // Set max value for healing
            progressBar.maxValue = maxHealingProgress;
            progressBar.minValue = 0f;
        }
    }
    
    void Update()
    {
        // Only allow healing if survivor is injured or dying
        if (survivorAgent == null || 
            (survivorAgent.GetHealthState() != SurvivorHealthState.Injured && 
             survivorAgent.GetHealthState() != SurvivorHealthState.Dying))
        {
            // Reset progress if survivor is now healthy
            if (healingProgress > 0)
            {
                healingProgress = 0f;
            }
            
            // Clear healers if target is no longer healable
            if (healingSurvivors.Count > 0)
            {
                // Unlock all healers
                foreach (GameObject healer in healingSurvivors)
                {
                    InteractionController healerController = healer.GetComponent<InteractionController>();
                    if (healerController != null)
                    {
                        InteractionController.UnlockCharacter(healerController);
                    }
                }
                
                // Unlock the target being healed
                InteractionController targetController = survivorAgent.GetComponent<InteractionController>();
                if (targetController != null)
                {
                    InteractionController.UnlockCharacter(targetController);
                }
                
                healingSurvivors.Clear();
            }
            
            if (progressCanvas != null)
                progressCanvas.SetActive(false);
                
            return;
        }
        
        int n = healingSurvivors.Count;
        
        // Freeze velocity of survivor being healed
        if (n > 0 && survivorAgent != null)
        {
            Rigidbody2D targetRb = survivorAgent.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                targetRb.linearVelocity = Vector2.zero;
            }
            
            // Also freeze healers' velocity
            foreach (GameObject healer in healingSurvivors)
            {
                Rigidbody2D healerRb = healer.GetComponent<Rigidbody2D>();
                if (healerRb != null)
                {
                    healerRb.linearVelocity = Vector2.zero;
                }
            }
        }
        
        // Healing (up to 2 survivors can heal at once)
        if (n > 0)
        {
            float totalHealingSpeed = Mathf.Min(n, 2) * baseHealingSpeed;
            float progressThisFrame = totalHealingSpeed * Time.deltaTime;
            healingProgress += progressThisFrame;
            
            // Reward healers for making progress
            foreach (GameObject healer in healingSurvivors)
            {
                SurvivorAgent healerAgent = healer.GetComponent<SurvivorAgent>();
                if (healerAgent != null)
                {
                    healerAgent.RewardHealingProgress(progressThisFrame);
                }
            }
            
            if (healingProgress >= maxHealingProgress)
            {
                CompleteHealing();
                return; // Exit early after completing
            }
        }
        
        // Clamp and update progress bar
        healingProgress = Mathf.Clamp(healingProgress, 0f, maxHealingProgress);
        
        // Only show progress bar when actively being healed
        if (progressCanvas != null)
        {
            bool shouldShow = n > 0;
            if (progressCanvas.activeSelf != shouldShow)
            {
                progressCanvas.SetActive(shouldShow);
            }
        }
        
        // Update progress bar value
        if (n > 0 && progressBar != null)
        {
            progressBar.value = healingProgress;
            progressBar.maxValue = maxHealingProgress;
            progressBar.minValue = 0f;
        }
        
        if (uiTransform != null && n > 0)
        {
            uiTransform.rotation = Quaternion.identity;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only survivors can heal other survivors
        SurvivorAgent otherSurvivor = other.GetComponent<SurvivorAgent>();
        
        if (otherSurvivor != null && otherSurvivor != survivorAgent)
        {
            survivorsInRange.Add(otherSurvivor);
            
            // Notify InteractionController
            InteractionController interactionController = otherSurvivor.GetComponent<InteractionController>();
            if (interactionController != null)
            {
                interactionController.OnHealTargetEnter(this);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        SurvivorAgent otherSurvivor = other.GetComponent<SurvivorAgent>();
        
        if (otherSurvivor != null)
        {
            survivorsInRange.Remove(otherSurvivor);
            StopHealing(otherSurvivor.gameObject);
            
            // Notify InteractionController
            InteractionController interactionController = otherSurvivor.GetComponent<InteractionController>();
            if (interactionController != null)
            {
                interactionController.OnHealTargetExit(this);
            }
        }
        
        if (survivorsInRange.Count == 0 && progressCanvas != null)
        {
            progressCanvas.SetActive(false);
        }
    }
    
    public void StartHealing(GameObject healer)
    {
        // Check if target can be healed
        if (survivorAgent == null || 
            (survivorAgent.GetHealthState() != SurvivorHealthState.Injured && 
             survivorAgent.GetHealthState() != SurvivorHealthState.Dying))
        {
            return;
        }
        
        // Check if healer is dying (cannot heal while dying)
        SurvivorAgent healerAgent = healer.GetComponent<SurvivorAgent>();
        if (healerAgent != null && healerAgent.GetHealthState() == SurvivorHealthState.Dying)
        {
            return;
        }
        
        // Check if already at max healers (2)
        if (healingSurvivors.Count >= 2)
        {
            return;
        }
        
        if (!healingSurvivors.Contains(healer))
        {
            healingSurvivors.Add(healer);
            
            // Lock the healer so they can't move while healing
            InteractionController healerController = healer.GetComponent<InteractionController>();
            if (healerController != null)
            {
                InteractionController.LockCharacter(healerController);
            }
            
            // Lock the survivor being healed so they can't move (use survivorAgent, not this)
            InteractionController targetController = survivorAgent.GetComponent<InteractionController>();
            if (targetController != null)
            {
                InteractionController.LockCharacter(targetController);
            }
        }
    }
    
    public void StopHealing(GameObject healer)
    {
        if (healingSurvivors.Contains(healer))
        {
            healingSurvivors.Remove(healer);
            
            // Unlock the healer
            InteractionController healerController = healer.GetComponent<InteractionController>();
            if (healerController != null)
            {
                InteractionController.UnlockCharacter(healerController);
            }
            
            // Unlock the survivor being healed if no one is healing them
            if (healingSurvivors.Count == 0)
            {
                InteractionController targetController = survivorAgent.GetComponent<InteractionController>();
                if (targetController != null)
                {
                    InteractionController.UnlockCharacter(targetController);
                }
                
                // Hide progress bar when healing stops
                if (progressCanvas != null)
                {
                    progressCanvas.SetActive(false);
                }
            }
        }
    }
    
    void CompleteHealing()
    {
        if (survivorAgent == null) return;
        
        SurvivorHealthState currentState = survivorAgent.GetHealthState();
        
        // Heal based on current state
        if (currentState == SurvivorHealthState.Dying)
        {
            // Dying -> Injured
            survivorAgent.SetHealthState(SurvivorHealthState.Injured);
        }
        else if (currentState == SurvivorHealthState.Injured)
        {
            // Injured -> Healthy
            survivorAgent.SetHealthState(SurvivorHealthState.Healthy);
        }
        
        // Reward and unlock all healers
        foreach (GameObject healer in healingSurvivors)
        {
            SurvivorAgent healerAgent = healer.GetComponent<SurvivorAgent>();
            if (healerAgent != null)
            {
                healerAgent.RewardCompletedHeal();
            }
            
            // Unlock the healer
            InteractionController healerController = healer.GetComponent<InteractionController>();
            if (healerController != null)
            {
                InteractionController.UnlockCharacter(healerController);
            }
        }
        
        // Unlock the healed survivor
        InteractionController targetController = survivorAgent.GetComponent<InteractionController>();
        if (targetController != null)
        {
            InteractionController.UnlockCharacter(targetController);
        }
        
        // Reset healing progress
        healingProgress = 0f;
        healingSurvivors.Clear();
        
        if (progressCanvas != null)
        {
            progressCanvas.SetActive(false);
        }
    }
    
    public bool CanBeHealed()
    {
        if (survivorAgent == null) return false;
        
        SurvivorHealthState state = survivorAgent.GetHealthState();
        return state == SurvivorHealthState.Injured || state == SurvivorHealthState.Dying;
    }
    
    public bool IsBeingHealed()
    {
        return healingSurvivors.Count > 0;
    }
    
    public int GetHealerCount()
    {
        return healingSurvivors.Count;
    }
}
