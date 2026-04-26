using UnityEngine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PalletController : MonoBehaviour
{
    private static HashSet<MonoBehaviour> globalLockedPlayers = new HashSet<MonoBehaviour>();
    
    [Header("Pallet States")]
    public GameObject standingObject;
    public GameObject droppedObject;
    
    [Header("Target Positions")]
    public Transform leftDropPosition;
    public Transform rightDropPosition;
    
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float velocityThreshold = 2.26f;
    public float lockDuration = 0.55f;
    public float dropZoneCheckRadius = 0.5f;
    
    private bool isDropped = false;
    private MonoBehaviour playerInLeftZone;
    private MonoBehaviour playerInRightZone;
    private bool leftZoneActive = true;
    private bool rightZoneActive = true;
    private MonoBehaviour lockedPlayer = null;
    private PalletThrowZone.ThrowSide? firstZoneEntered = null;
    private float zoneResetTimer = 0f;
    private const float zoneResetDelay = 1f;
    
    void Start()
    {
        if (standingObject != null)
            standingObject.SetActive(true);
        
        if (droppedObject != null)
            droppedObject.SetActive(false);
    }
    
    void Update()
    {
        if (isDropped)
            return;
        
        // Input handling moved to InteractionController
        
        CheckPlayerVelocity(playerInLeftZone, ref leftZoneActive);
        CheckPlayerVelocity(playerInRightZone, ref rightZoneActive);
        
        if (firstZoneEntered != null && playerInLeftZone == null && playerInRightZone == null)
        {
            zoneResetTimer += Time.deltaTime;
            if (zoneResetTimer >= zoneResetDelay)
            {
                firstZoneEntered = null;
                zoneResetTimer = 0f;
            }
        }
        else
        {
            zoneResetTimer = 0f;
        }
    }
    
    public static bool IsPlayerLocked(MonoBehaviour player)
    {
        return globalLockedPlayers.Contains(player);
    }
    
    public void OnPlayerEnterZone(MonoBehaviour player, PalletThrowZone.ThrowSide side)
    {
        if (isDropped)
            return;
        
        if (!player.CompareTag("Survivor"))
            return;
        
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        bool playerMovingFast = rb != null && rb.linearVelocity.magnitude > velocityThreshold;
        
        if (side == PalletThrowZone.ThrowSide.Left)
        {
            playerInLeftZone = player;
            
            if (firstZoneEntered == null)
            {
                firstZoneEntered = PalletThrowZone.ThrowSide.Left;
                if (playerMovingFast)
                {
                    leftZoneActive = false;
                }
            }
        }
        else if (side == PalletThrowZone.ThrowSide.Right)
        {
            playerInRightZone = player;
            
            if (firstZoneEntered == null)
            {
                firstZoneEntered = PalletThrowZone.ThrowSide.Right;
                if (playerMovingFast)
                {
                    rightZoneActive = false;
                }
            }
        }
    }
    
    public void OnPlayerExitZone(MonoBehaviour player, PalletThrowZone.ThrowSide side)
    {
        if (side == PalletThrowZone.ThrowSide.Left && playerInLeftZone == player)
        {
            playerInLeftZone = null;
            leftZoneActive = true;
        }
        else if (side == PalletThrowZone.ThrowSide.Right && playerInRightZone == player)
        {
            playerInRightZone = null;
            rightZoneActive = true;
        }
    }
    
    private void CheckPlayerVelocity(MonoBehaviour player, ref bool zoneActive)
    {
        if (player == null)
        {
            return;
        }
        
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            bool isFirstZone = (player == playerInLeftZone && firstZoneEntered == PalletThrowZone.ThrowSide.Left) ||
                               (player == playerInRightZone && firstZoneEntered == PalletThrowZone.ThrowSide.Right);
            
            if (isFirstZone)
            {
                if (rb.linearVelocity.magnitude > velocityThreshold)
                {
                    zoneActive = false;
                }
                else
                {
                    zoneActive = true;
                }
            }
        }
    }
    
    private bool IsKillerAtPosition(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, dropZoneCheckRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Killer"))
            {
                return true;
            }
        }
        return false;
    }
    
    public bool TryDropPalletFromInteraction(GameObject interactingObject)
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
            return TryDropPallet(player);
        }
        return false;
    }
    
    private bool TryDropPallet(MonoBehaviour player = null)
    {
        Debug.Log($"[PalletController] TryDropPallet called for {player?.GetType().Name}");
        
        if (lockedPlayer != null)
        {
            Debug.Log("[PalletController] Locked player exists, cannot drop");
            return false;
        }
        
        Transform targetPosition = null;
        MonoBehaviour playerToMove = null;
        
        Debug.Log($"[PalletController] playerInLeftZone: {playerInLeftZone?.GetType().Name}, leftZoneActive: {leftZoneActive}");
        Debug.Log($"[PalletController] playerInRightZone: {playerInRightZone?.GetType().Name}, rightZoneActive: {rightZoneActive}");
        
        // If player is specified, only drop pallet for that specific player
        if (player != null)
        {
            Debug.Log($"[PalletController] Checking if player {player.GetType().Name} is in zones");
            Debug.Log($"[PalletController] player == playerInLeftZone: {player == playerInLeftZone}");
            Debug.Log($"[PalletController] player == playerInRightZone: {player == playerInRightZone}");
            
            if (player == playerInLeftZone && leftZoneActive && !IsKillerAtPosition(leftDropPosition.position))
            {
                targetPosition = leftDropPosition;
                playerToMove = playerInLeftZone;
                Debug.Log("[PalletController] Left zone conditions met");
            }
            else if (player == playerInRightZone && rightZoneActive && !IsKillerAtPosition(rightDropPosition.position))
            {
                targetPosition = rightDropPosition;
                playerToMove = playerInRightZone;
                Debug.Log("[PalletController] Right zone conditions met");
            }
        }
        else
        {
            // Try left zone first, then right
            if (playerInLeftZone != null && leftZoneActive && !IsKillerAtPosition(leftDropPosition.position))
            {
                targetPosition = leftDropPosition;
                playerToMove = playerInLeftZone;
            }
            else if (playerInRightZone != null && rightZoneActive && !IsKillerAtPosition(rightDropPosition.position))
            {
                targetPosition = rightDropPosition;
                playerToMove = playerInRightZone;
            }
        }
        
        
        if (targetPosition != null && playerToMove != null)
        {
            lockedPlayer = playerToMove;
            InteractionController.LockCharacter(playerToMove);
            StartCoroutine(MovePlayerAndDropPallet(playerToMove, targetPosition));
            return true;
        }
        return false;
    }
    
    private IEnumerator MovePlayerAndDropPallet(MonoBehaviour player, Transform targetPosition)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
        
        Transform playerTransform = player.transform;
        Vector3 startPosition = playerTransform.position;
        Vector3 endPosition = targetPosition.position;
        
        float distance = Vector3.Distance(startPosition, endPosition);
        float duration = distance / moveSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }
            
            yield return null;
        }
        
        playerTransform.position = endPosition;
        
        if (standingObject != null)
            standingObject.SetActive(false);
        
        if (droppedObject != null)
            droppedObject.SetActive(true);
        
        float lockElapsed = 0f;
        while (lockElapsed < lockDuration)
        {
            lockElapsed += Time.deltaTime;
            
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerTransform.position = endPosition;
            }
            
            yield return null;
        }
        
        
        isDropped = true;
        lockedPlayer = null;
        InteractionController.UnlockCharacter(player);
    }
}
