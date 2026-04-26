using UnityEngine;

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class KillerAgent : Agent
{
    // TODO: Implement KillerAgent following the same pattern as SurvivorAgent
    // 
    // Required components:
    // 1. Movement Settings (acceleration, deceleration, maxSpeed)
    // 2. Raycast Settings (raycastDistance, numCircleRaycasts, raycastLayerMask)
    // 3. InteractionController component (for vaulting, breaking pallets, etc.)
    //
    // Observations needed:
    // - Own velocity (2 observations)
    // - 16 raycasts in circle detecting: Wall, ShortWall, Generator, Window, Pallet, Survivor (6 types = 96 observations)
    // - Last known survivor positions (one set per survivor, e.g., 4 survivors × 3 values = 12 observations)
    //   - Last known position relative to killer (2 values: x, y offset)
    //   - Time since last seen (1 value)
    //
    // Actions needed:
    // - Continuous Actions: Horizontal movement, Vertical movement (2 actions)
    // - Discrete Actions: Interact button for vaulting/breaking pallets (1 action, branch size 2)
    //
    // Key differences from SurvivorAgent:
    // - Killer can see Survivors in raycasts (add Survivor tag detection)
    // - Killer vaults slower (handled by Vaultable.cs based on "Killer" tag)
    // - Killer can break pallets (add this interaction to InteractionController if needed)
    // - Needs "last known position" tracking for each survivor
    //
    // Reward structure:
    // - Positive reward for catching survivors
    // - Positive reward for damaging generators (causing regression)
    // - Small negative reward per step to encourage efficiency
    // - Positive reward for breaking pallets / hitting survivors through pallets
    //
    // Implementation steps:
    // 1. Copy movement code from SurvivorAgent.cs
    // 2. Update CollectObservations to include Survivor detection in raycasts
    // 3. Add last known position tracking (store Vector2 and float for each survivor)
    // 4. Add InteractionController component reference
    // 5. Implement OnActionReceived with movement + interaction
    // 6. Implement Heuristic for manual testing
    // 7. Add reward methods (RewardCatch, RewardPalletBreak, etc.)
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}

