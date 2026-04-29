using UnityEngine;

public class VaultZone : MonoBehaviour
{
    public enum VaultSide { Left, Right }
    public VaultSide side;
    
    private Vaultable vaultable;
    
    void Start()
    {
        vaultable = GetComponentInParent<Vaultable>();
        if (vaultable == null)
        {
            Debug.LogError("VaultZone could not find Vaultable in parent!");
        }
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
        
        if (player != null && vaultable != null)
        {
            vaultable.OnPlayerEnterVaultZone(player, side);
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
        
        if (player != null && vaultable != null)
        {
            vaultable.OnPlayerExitVaultZone(player, side);
        }
    }
}
