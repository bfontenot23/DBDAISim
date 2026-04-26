using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Vaultable : MonoBehaviour
{
    private static HashSet<MonoBehaviour> globalVaultingPlayers = new HashSet<MonoBehaviour>();
    
    [Header("Vault Zones")]
    public GameObject leftVaultZone;
    public GameObject rightVaultZone;
    
    [Header("Vault Settings")]
    public float slowVaultDuration = 2f;
    public float mediumVaultDuration = 1.1f;
    public float fastVaultDuration = 1.1f;
    public float killerVaultDuration = 1.7f;
    public float mediumVaultThreshold = 2.26f;
    public float fastVaultThreshold = 4f;
    public float snapSpeed = 10f;
    
    [Header("Vault Type")]
    public bool allowKillerVault = true;
    
    private MonoBehaviour playerInLeftVaultZone;
    private MonoBehaviour playerInRightVaultZone;
    private MonoBehaviour vaultingPlayer = null;
    
    public static bool IsPlayerVaulting(MonoBehaviour player)
    {
        return globalVaultingPlayers.Contains(player);
    }
    
    void Update()
    {
        // Input handling moved to InteractionController
    }
    
    public void OnPlayerEnterVaultZone(MonoBehaviour player, VaultZone.VaultSide side)
    {
        if (!allowKillerVault && player.CompareTag("Killer"))
            return;
        
        if (side == VaultZone.VaultSide.Left)
        {
            playerInLeftVaultZone = player;
        }
        else if (side == VaultZone.VaultSide.Right)
        {
            playerInRightVaultZone = player;
        }
    }
    
    public void OnPlayerExitVaultZone(MonoBehaviour player, VaultZone.VaultSide side)
    {
        if (side == VaultZone.VaultSide.Left && playerInLeftVaultZone == player)
        {
            playerInLeftVaultZone = null;
        }
        else if (side == VaultZone.VaultSide.Right && playerInRightVaultZone == player)
        {
            playerInRightVaultZone = null;
        }
    }
    
    public bool TryVaultFromInteraction(GameObject interactingObject)
    {
        MonoBehaviour player = interactingObject.GetComponent<SurvivorAgent>();
        if (player == null)
        {
            player = interactingObject.GetComponent<PlayerController>();
        }
        if (player == null)
        {
            player = interactingObject.GetComponent<KillerAgent>();
        }
        
        if (player != null)
        {
            return TryVault(player);
        }
        return false;
    }
    
    private bool TryVault(MonoBehaviour player = null)
    {
        if (vaultingPlayer != null)
            return false;
        
        Transform startZone = null;
        Transform endZone = null;
        MonoBehaviour playerToVault = null;
        
        // If player is specified, only vault that specific player
        if (player != null)
        {
            if (player == playerInLeftVaultZone && leftVaultZone != null && rightVaultZone != null)
            {
                startZone = leftVaultZone.transform;
                endZone = rightVaultZone.transform;
                playerToVault = playerInLeftVaultZone;
            }
            else if (player == playerInRightVaultZone && rightVaultZone != null && leftVaultZone != null)
            {
                startZone = rightVaultZone.transform;
                endZone = leftVaultZone.transform;
                playerToVault = playerInRightVaultZone;
            }
        }
        else
        {
            // Try left zone first, then right
            if (playerInLeftVaultZone != null && leftVaultZone != null && rightVaultZone != null)
            {
                startZone = leftVaultZone.transform;
                endZone = rightVaultZone.transform;
                playerToVault = playerInLeftVaultZone;
            }
            else if (playerInRightVaultZone != null && rightVaultZone != null && leftVaultZone != null)
            {
                startZone = rightVaultZone.transform;
                endZone = leftVaultZone.transform;
                playerToVault = playerInRightVaultZone;
            }
        }
        
        
        if (startZone != null && endZone != null && playerToVault != null)
        {
            vaultingPlayer = playerToVault;
            InteractionController.StartVaulting(playerToVault);
            StartCoroutine(PerformVault(playerToVault, startZone.position, endZone.position));
            return true;
        }
        return false;
    }
    
    private IEnumerator PerformVault(MonoBehaviour player, Vector3 startPosition, Vector3 endPosition)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        float velocityBeforeVault = 0f;
        
        if (playerRb != null)
        {
            velocityBeforeVault = playerRb.linearVelocity.magnitude;
            playerRb.linearVelocity = Vector2.zero;
        }
        
        float vaultDuration = GetVaultDuration(player, velocityBeforeVault);
        
        Transform playerTransform = player.transform;
        Vector3 playerStartPosition = playerTransform.position;
        
        float moveToStartDistance = Vector3.Distance(playerStartPosition, startPosition);
        float moveToStartDuration = moveToStartDistance / snapSpeed;
        float elapsed = 0f;
        
        while (elapsed < moveToStartDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToStartDuration;
            playerTransform.position = Vector3.Lerp(playerStartPosition, startPosition, t);
            
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }
            
            yield return null;
        }
        
        playerTransform.position = startPosition;
        
        elapsed = 0f;
        while (elapsed < vaultDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vaultDuration;
            playerTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }
            
            yield return null;
        }
        
        playerTransform.position = endPosition;
        
        if (playerRb != null)
        {
            Vector3 vaultDirection = (endPosition - startPosition).normalized;
            float exitVelocity = Mathf.Min(velocityBeforeVault, GetMaxSpeed(player));
            playerRb.linearVelocity = new Vector2(vaultDirection.x, vaultDirection.y) * exitVelocity;
        }
        
        vaultingPlayer = null;
        InteractionController.StopVaulting(player);
    }
    
    private float GetVaultDuration(MonoBehaviour player, float velocity)
    {
        if (player.CompareTag("Killer"))
        {
            return killerVaultDuration;
        }
        
        if (velocity < mediumVaultThreshold)
        {
            return slowVaultDuration;
        }
        else if (velocity < fastVaultThreshold)
        {
            return mediumVaultDuration;
        }
        else
        {
            return fastVaultDuration;
        }
    }
    
    private float GetMaxSpeed(MonoBehaviour player)
    {
        // Try to get maxSpeed from PlayerController
        PlayerController pc = player as PlayerController;
        if (pc != null)
        {
            return pc.maxSpeed;
        }
        
        // Try to get maxSpeed from SurvivorAgent
        SurvivorAgent sa = player as SurvivorAgent;
        if (sa != null)
        {
            return sa.maxSpeed;
        }
        
        // Default fallback
        return 4f;
    }
}
