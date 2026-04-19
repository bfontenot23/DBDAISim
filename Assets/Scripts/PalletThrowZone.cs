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
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && palletController != null)
        {
            palletController.OnPlayerEnterZone(player, side);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && palletController != null)
        {
            palletController.OnPlayerExitZone(player, side);
        }
    }
}
