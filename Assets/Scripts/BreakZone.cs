using UnityEngine;

public class BreakZone : MonoBehaviour
{
    public enum BreakSide { Left, Right }
    public BreakSide side;
    
    private PalletController palletController;
    
    void Start()
    {
        palletController = GetComponentInParent<PalletController>();
        if (palletController == null)
        {
            Debug.LogError("BreakZone could not find PalletController in parent!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only killers can break pallets
        KillerAgent killer = other.GetComponent<KillerAgent>();
        
        if (killer != null && palletController != null)
        {
            palletController.OnKillerEnterBreakZone(killer, side);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Only killers can break pallets
        KillerAgent killer = other.GetComponent<KillerAgent>();
        
        if (killer != null && palletController != null)
        {
            palletController.OnKillerExitBreakZone(killer, side);
        }
    }
}
