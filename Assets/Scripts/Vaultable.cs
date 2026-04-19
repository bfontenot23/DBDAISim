using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Vaultable : MonoBehaviour
{
    private static HashSet<PlayerController> globalVaultingPlayers = new HashSet<PlayerController>();
    
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
    
    private PlayerController playerInLeftVaultZone;
    private PlayerController playerInRightVaultZone;
    private PlayerController vaultingPlayer = null;
    
    public static bool IsPlayerVaulting(PlayerController player)
    {
        return globalVaultingPlayers.Contains(player);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryVault();
        }
    }
    
    public void OnPlayerEnterVaultZone(PlayerController player, VaultZone.VaultSide side)
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
    
    public void OnPlayerExitVaultZone(PlayerController player, VaultZone.VaultSide side)
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
    
    private void TryVault()
    {
        if (vaultingPlayer != null)
            return;
        
        Transform startZone = null;
        Transform endZone = null;
        PlayerController playerToVault = null;
        
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
        
        if (startZone != null && endZone != null && playerToVault != null)
        {
            vaultingPlayer = playerToVault;
            globalVaultingPlayers.Add(playerToVault);
            StartCoroutine(PerformVault(playerToVault, startZone.position, endZone.position));
        }
    }
    
    private IEnumerator PerformVault(PlayerController player, Vector3 startPosition, Vector3 endPosition)
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
            float exitVelocity = Mathf.Min(velocityBeforeVault, player.maxSpeed);
            playerRb.linearVelocity = new Vector2(vaultDirection.x, vaultDirection.y) * exitVelocity;
        }
        
        vaultingPlayer = null;
        globalVaultingPlayers.Remove(player);
    }
    
    private float GetVaultDuration(PlayerController player, float velocity)
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
}
