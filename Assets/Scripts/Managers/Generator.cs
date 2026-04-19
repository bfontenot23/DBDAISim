using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Generator : MonoBehaviour
{
    [Header("Progress")]
    public float progress = 0f;
    public float maxProgress = 100f;
    public float baseRepairSpeed = 1f;
    private List<GameObject> repairingPlayers = new List<GameObject>();
    private bool isComplete = false;

    [Header("Regression")]
    public bool isRegressing = false;
    public float regressionSpeed = 0.25f;
    private float repairSinceRegression = 0f;
    public float requiredRepairToStopRegression = 5f;

    // Update is called once per frame
    void Update()
    {
        if (isComplete) return;

        int n = repairingPlayers.Count;
        //repairing
        if (n > 0)
        {
            float efficiency = GetEfficiency(n);
            float totalRepairSpeed = n * efficiency * baseRepairSpeed;
            progress += totalRepairSpeed * Time.deltaTime;
           
            if (isRegressing)
            {
                repairSinceRegression += totalRepairSpeed * Time.deltaTime;
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
        else if (isRegressing && !isComplete)
        {
            progress -= regressionSpeed * Time.deltaTime; 
        }
        progress = Mathf.Clamp(progress, 0f, maxProgress);
    }

    float GetEfficiency(int survivors)
    {
        if (survivors <= 1) return 1f;
        float penalty = 0.15f * (survivors - 1);
        return Mathf.Clamp01(1f - penalty);
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
        progress = maxProgress;
        Debug.Log("Generator complete!");
        repairingPlayers.Clear();
        // Implement generator completion logic here (e.g., open exit, trigger events)

    }

    public void StartRegression()
    {
        isRegressing = true;
        repairSinceRegression = 0f;
    }

}
