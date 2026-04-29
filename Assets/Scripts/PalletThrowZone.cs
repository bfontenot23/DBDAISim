using UnityEngine;

public class PalletThrowZone : MonoBehaviour
{
    public enum ThrowSide { Left, Right }
    public ThrowSide side;
    
    private PalletController palletController;
    
    void Start()
    {
        palletController = GetComponentInParent<PalletController>();
        if (palletController == null)
        {
            Debug.LogError("PalletThrowZone could not find PalletController in parent!");
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
        
        if (player != null && palletController != null)
        {
            Debug.Log($"[PalletThrowZone] {player.GetType().Name} entered {side} zone");
            palletController.OnPlayerEnterZone(player, side);
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
        
        if (player != null && palletController != null)
        {
            palletController.OnPlayerExitZone(player, side);
        }
    }
}
