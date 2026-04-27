using UnityEngine;
using System.Collections.Generic;

public class InteractionController : MonoBehaviour
{
    private static HashSet<MonoBehaviour> globalLockedCharacters = new HashSet<MonoBehaviour>();
    private static HashSet<MonoBehaviour> globalVaultingCharacters = new HashSet<MonoBehaviour>();
    
    private Rigidbody2D rb;
    private bool isRepairing = false;
    private Generator currentGenerator = null;
    private bool isHealing = false;
    private SurvivorHealing currentHealTarget = null;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    public static bool IsCharacterLocked(MonoBehaviour character)
    {
        return globalLockedCharacters.Contains(character);
    }
    
    public static bool IsCharacterVaulting(MonoBehaviour character)
    {
        return globalVaultingCharacters.Contains(character);
    }
    
    public static void LockCharacter(MonoBehaviour character)
    {
        globalLockedCharacters.Add(character);
    }
    
    public static void UnlockCharacter(MonoBehaviour character)
    {
        globalLockedCharacters.Remove(character);
    }
    
    public static void StartVaulting(MonoBehaviour character)
    {
        globalVaultingCharacters.Add(character);
    }
    
    public static void StopVaulting(MonoBehaviour character)
    {
        globalVaultingCharacters.Remove(character);
    }
    
    public void TryInteract()
    {
        if (IsCharacterLocked(this) || IsCharacterVaulting(this))
        {
            Debug.Log("[InteractionController] Character is locked or vaulting, cannot interact");
            return;
        }
        
        Debug.Log("[InteractionController] TryInteract called");
        
        // Check if this is a killer trying to kick a generator or break a pallet
        if (gameObject.CompareTag("Killer"))
        {
            // Try to break pallet first
            PalletController[] pallets = FindObjectsByType<PalletController>(FindObjectsSortMode.None);
            Debug.Log($"[InteractionController] Killer found {pallets.Length} pallets");
            foreach (PalletController pallet in pallets)
            {
                if (pallet.TryBreakPalletFromInteraction(gameObject))
                {
                    Debug.Log("[InteractionController] Killer started breaking pallet");
                    return;
                }
            }
            
            // If no pallet to break, try to kick generator
            if (currentGenerator != null)
            {
                currentGenerator.KickGenerator(gameObject);
                Debug.Log("[InteractionController] Killer kicked generator");
                return;
            }
        }
        
        // Try to vault first
        Vaultable[] vaultables = FindObjectsByType<Vaultable>(FindObjectsSortMode.None);
        Debug.Log($"[InteractionController] Found {vaultables.Length} vaultables");
        foreach (Vaultable vaultable in vaultables)
        {
            if (vaultable.TryVaultFromInteraction(gameObject))
            {
                Debug.Log("[InteractionController] Vault successful!");
                return;
            }
        }
        
        // Try to drop pallet
        PalletController[] palletsForDrop = FindObjectsByType<PalletController>(FindObjectsSortMode.None);
        Debug.Log($"[InteractionController] Found {palletsForDrop.Length} pallets");
        foreach (PalletController pallet in palletsForDrop)
        {
            if (pallet.TryDropPalletFromInteraction(gameObject))
            {
                Debug.Log("[InteractionController] Pallet drop successful!");
                return;
            }
        }
        
        // Try to heal another survivor (only survivors can heal)
        if (gameObject.CompareTag("Survivor") && currentHealTarget != null)
        {
            // Check if this survivor can heal (not dying)
            SurvivorAgent healerAgent = GetComponent<SurvivorAgent>();
            if (healerAgent != null && healerAgent.GetHealthState() != SurvivorHealthState.Dying)
            {
                if (currentHealTarget.CanBeHealed())
                {
                    Debug.Log("[InteractionController] Starting to heal another survivor");
                    StartHealing();
                    return;
                }
            }
        }
        
        Debug.Log("[InteractionController] No valid interaction found");
    }
    
    public void StartGeneratorRepair()
    {
        if (currentGenerator != null && !isRepairing)
        {
            isRepairing = true;
            currentGenerator.StartRepair(gameObject);
        }
    }
    
    public void StopGeneratorRepair()
    {
        if (currentGenerator != null && isRepairing)
        {
            isRepairing = false;
            currentGenerator.StopRepair(gameObject);
        }
    }
    
    public void OnGeneratorEnter(Generator generator)
    {
        currentGenerator = generator;
    }
    
    public void OnGeneratorExit(Generator generator)
    {
        if (currentGenerator == generator)
        {
            StopGeneratorRepair();
            currentGenerator = null;
        }
    }
    
    public bool IsRepairingGenerator()
    {
        return isRepairing;
    }
    
    public Generator GetCurrentGenerator()
    {
        return currentGenerator;
    }
    
    public void StartHealing()
    {
        if (currentHealTarget != null && !isHealing)
        {
            isHealing = true;
            currentHealTarget.StartHealing(gameObject);
        }
    }
    
    public void StopHealing()
    {
        if (currentHealTarget != null && isHealing)
        {
            isHealing = false;
            currentHealTarget.StopHealing(gameObject);
        }
    }
    
    public void OnHealTargetEnter(SurvivorHealing healTarget)
    {
        currentHealTarget = healTarget;
    }
    
    public void OnHealTargetExit(SurvivorHealing healTarget)
    {
        if (currentHealTarget == healTarget)
        {
            StopHealing();
            currentHealTarget = null;
        }
    }
    
    public bool IsHealing()
    {
        return isHealing;
    }
    
    public SurvivorHealing GetCurrentHealTarget()
    {
        return currentHealTarget;
    }
}
