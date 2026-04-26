using UnityEngine;
using System.Collections.Generic;

public class InteractionController : MonoBehaviour
{
    private static HashSet<MonoBehaviour> globalLockedCharacters = new HashSet<MonoBehaviour>();
    private static HashSet<MonoBehaviour> globalVaultingCharacters = new HashSet<MonoBehaviour>();
    
    private Rigidbody2D rb;
    private bool isRepairing = false;
    private Generator currentGenerator = null;
    
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
        PalletController[] pallets = FindObjectsByType<PalletController>(FindObjectsSortMode.None);
        Debug.Log($"[InteractionController] Found {pallets.Length} pallets");
        foreach (PalletController pallet in pallets)
        {
            if (pallet.TryDropPalletFromInteraction(gameObject))
            {
                Debug.Log("[InteractionController] Pallet drop successful!");
                return;
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
}
