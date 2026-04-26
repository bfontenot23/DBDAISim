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

    void Start()
    {
        progressBar = GetComponentInChildren<Slider>();
        if (progressBar != null)
        {
            progressCanvas = progressBar.transform.parent.gameObject; 
            progressCanvas.SetActive(false);

            uiTransform = progressCanvas.transform; 
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
            
            // Penalize killer for generator progress
            GameObject killer = GameObject.FindGameObjectWithTag("Killer");
            if (killer != null)
            {
                KillerAgent killerAgent = killer.GetComponent<KillerAgent>();
                if (killerAgent != null)
                {
                    killerAgent.PenalizeGeneratorProgress(progressThisFrame);
                }
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
        //Debug.Log("Generator complete!");
        repairingPlayers.Clear();

        if (progressCanvas != null)
        {
            progressCanvas.SetActive(true);
        }
        // Implement generator completion logic here (e.g., open exit, trigger events)

    }

    public void StartRegression()
    {
        if (isComplete) return;
        isRegressing = true;
        repairSinceRegression = 0f;
    }
    
    public void KickGenerator(GameObject kicker)
    {
        if (isComplete) return;
        
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

}
