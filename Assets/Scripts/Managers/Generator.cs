using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Generator : MonoBehaviour
{
    [Header("Progress")]
    public float progress = 0f;
    public float maxProgress = 100f;
    public float baseRepairSpeed = 1f;
    private List<GameObject> repairingPlayers = new List<GameObject>();
    private HashSet<MonoBehaviour> playersInRange = new HashSet<MonoBehaviour>();
    private HashSet<GameObject> survivorsWhoStartedGenerator = new HashSet<GameObject>();
    private bool isComplete = false;

    [Header("Regression")]
    public bool isRegressing = false;
    public float regressionSpeed = 0.25f;
    private float repairSinceRegression = 0f;
    public float requiredRepairToStopRegression = 5f;

    [Header("UI")]
    private Slider progressBar; 
    private GameObject progressCanvas; 
    private Transform uiTransform;
    
    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    private Color baseColor = new Color(0x52 / 255f, 0x52 / 255f, 0x52 / 255f); // #525252
    private Color normalProgressColor = new Color(0xFF / 255f, 0xF3 / 255f, 0x00 / 255f); // #FFF300
    private Color regressionProgressColor = new Color(0xFF / 255f, 0x85 / 255f, 0x00 / 255f); // #FF8500
    
    private Transform environmentRoot;

    void Start()
    {
        progressBar = GetComponentInChildren<Slider>();
        if (progressBar != null)
        {
            progressCanvas = progressBar.transform.parent.gameObject; 
            progressCanvas.SetActive(false);

            uiTransform = progressCanvas.transform; 
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
        
        // Find the environment root (Map_X parent)
        environmentRoot = transform.parent;
        if (environmentRoot == null)
        {
            Debug.LogWarning("[Generator] No parent found! Generator should be child of a Map.");
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (isComplete)
        {
            if (progressCanvas != null)
                progressCanvas.SetActive(true);

            if (progressBar != null)
                progressBar.value = maxProgress;
            
            if (uiTransform != null)
                uiTransform.rotation = Quaternion.identity; 
            
            return;
        }

        //Debug.Log($"Repairing: {repairingPlayers.Count}, Progress: {progress:F2}, Regressing: {isRegressing}");
        

        
        int n = repairingPlayers.Count;
        //repairing
        if (n > 0)
        {
            float efficiency = GetEfficiency(n);
            float totalRepairSpeed = n * efficiency * baseRepairSpeed;
            float progressThisFrame = totalRepairSpeed * Time.deltaTime;
            progress += progressThisFrame;
            
            // Reward survivors for generator progress
            foreach (GameObject player in repairingPlayers)
            {
                SurvivorAgent survivor = player.GetComponent<SurvivorAgent>();
                if (survivor != null)
                {
                    // Give first-time start reward
                    if (!survivorsWhoStartedGenerator.Contains(player))
                    {
                        survivor.RewardStartGenerator();
                        survivorsWhoStartedGenerator.Add(player);
                    }
                    
                    // Give ongoing progress reward
                    survivor.RewardGeneratorProgress(progressThisFrame);
                }
            }
            
            // Penalize killer for generator progress
            KillerAgent killerAgent = environmentRoot != null ? 
                environmentRoot.GetComponentInChildren<KillerAgent>() : 
                null;
            if (killerAgent != null)
            {
                killerAgent.PenalizeGeneratorProgress(progressThisFrame);
            }
           
            if (isRegressing)
            {
                repairSinceRegression += progressThisFrame;
                if (repairSinceRegression >= requiredRepairToStopRegression)
                {
                    isRegressing = false;
                    repairSinceRegression = 0f;
                }
            }

            if (progress >= maxProgress)
            {
                CompleteGeneration();
            }
        }
        else if (isRegressing)
        {
            progress -= regressionSpeed * Time.deltaTime; 
        }
        progress = Mathf.Clamp(progress, 0f, maxProgress);
        if (progressBar != null)
        {
            progressBar.value = progress; 
        }

        if (progressCanvas != null)
        {
            if (isComplete)
            {
                progressCanvas.SetActive(true);
            }
            else
            {
                progressCanvas.SetActive(repairingPlayers.Count > 0);
            }
        }

        if (uiTransform != null)
        {
            uiTransform.rotation = Quaternion.identity; 
        }
        
        // Update generator color based on progress
        UpdateGeneratorColor();
    }

    float GetEfficiency(int survivors)
    {
        if (survivors <= 1) return 1f;
        float penalty = 0.15f * (survivors - 1);
        return Mathf.Clamp01(1f - penalty);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MonoBehaviour player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponent<SurvivorAgent>();
        }
        if (player == null)
        {
            player = other.GetComponent<KillerAgent>();
        }
        
        if (player != null)
        {
            playersInRange.Add(player);
            
            // Notify InteractionController
            InteractionController interactionController = player.GetComponent<InteractionController>();
            if (interactionController != null)
            {
                interactionController.OnGeneratorEnter(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        MonoBehaviour player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponent<SurvivorAgent>();
        }
        if (player == null)
        {
            player = other.GetComponent<KillerAgent>();
        }
        
        if (player != null)
        {
            playersInRange.Remove(player);
            StopRepair(player.gameObject);
            
            // Notify InteractionController
            InteractionController interactionController = player.GetComponent<InteractionController>();
            if (interactionController != null)
            {
                interactionController.OnGeneratorExit(this);
            }
        }
        
        if (playersInRange.Count == 0 && progressCanvas != null)
        {
            progressCanvas.SetActive(false);
        }
    }

    public void StartRepair(GameObject player)
    {
        if (isComplete) return;

        if (!repairingPlayers.Contains(player))
        {
            repairingPlayers.Add(player);
        }
    }
    public void StopRepair(GameObject player)
    {
        if (repairingPlayers.Contains(player))
        {
            repairingPlayers.Remove(player);
        }
    }
    void CompleteGeneration()
    {
        isComplete = true;
        isRegressing = false;
        progress = maxProgress;
        repairingPlayers.Clear();

        if (progressCanvas != null)
        {
            progressCanvas.SetActive(true);
        }
        
        // Check if enough generators are completed for survivors to win
        CheckGeneratorCompletionWinCondition();
    }
    
    private void CheckGeneratorCompletionWinCondition()
    {
        if (environmentRoot == null) return;
        
        // Count completed generators
        Generator[] allGenerators = environmentRoot.GetComponentsInChildren<Generator>();
        int completedCount = 0;
        
        foreach (Generator gen in allGenerators)
        {
            if (gen.isComplete)
            {
                completedCount++;
            }
        }
        
        // Win condition: 5 generators OR all available generators (whichever comes first)
        int requiredGenerators = Mathf.Min(5, allGenerators.Length);
        
        if (completedCount >= requiredGenerators)
        {
            // Survivors win! Reward all survivors and penalize killer
            SurvivorAgent[] allSurvivors = environmentRoot.GetComponentsInChildren<SurvivorAgent>(true);
            foreach (SurvivorAgent survivor in allSurvivors)
            {
                if (!survivor.isEliminated)
                {
                    survivor.RewardGeneratorsCompleted();
                }
            }
            
            KillerAgent killerAgent = environmentRoot.GetComponentInChildren<KillerAgent>();
            if (killerAgent != null)
            {
                killerAgent.PenalizeGeneratorsCompleted();
            }
            
            // Use MapEnvironmentController to end episode
            MapEnvironmentController envController = environmentRoot.GetComponent<MapEnvironmentController>();
            if (envController != null)
            {
                envController.OnSurvivorsEscape();
            }
        }
    }

    public void StartRegression()
    {
        if (isComplete) return;
        isRegressing = true;
        repairSinceRegression = 0f;
    }
    
    public void KickGenerator(GameObject kicker)
    {
        if (isComplete)
        {
            Debug.Log("[Generator] Cannot kick a completed generator");
            return;
        }
        
        // Can't kick a generator with no progress
        if (progress <= 0f)
        {
            Debug.Log("[Generator] Cannot kick generator with 0 progress");
            return;
        }
        
        // Can't kick a generator that's already regressing
        if (isRegressing)
        {
            Debug.Log("[Generator] Cannot kick generator that is already regressing");
            return;
        }
        
        // Lock the killer for 2.34 seconds, then start regression
        KillerAgent killerAgent = kicker.GetComponent<KillerAgent>();
        if (killerAgent != null)
        {
            killerAgent.StartCoroutine(killerAgent.PerformGeneratorKick(this));
        }
    }
    
    void UpdateGeneratorColor()
    {
        if (spriteRenderer == null) return;
        
        float progressRatio = Mathf.Clamp01(progress / maxProgress);
        Color targetColor = isRegressing ? regressionProgressColor : normalProgressColor;
        Color currentColor = Color.Lerp(baseColor, targetColor, progressRatio);
        
        spriteRenderer.color = currentColor;
    }

}
