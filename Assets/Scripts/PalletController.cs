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
    public float stunDuration = 2.0f;
    public float stunCheckRadius = 1.5f;
    
    private bool isDropped = false;
    private MonoBehaviour playerInLeftZone;
    private MonoBehaviour playerInRightZone;
    private bool leftZoneActive = true;
    private bool rightZoneActive = true;
    private MonoBehaviour lockedPlayer = null;
    private PalletThrowZone.ThrowSide? firstZoneEntered = null;
    private float zoneResetTimer = 0f;
    private const float zoneResetDelay = 1f;
    
    // Break zone tracking for killer
    private KillerAgent killerInLeftBreakZone;
    private KillerAgent killerInRightBreakZone;
    
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
        {
            // If pallet is dropped, allow killer to break it
            if (player.CompareTag("Killer"))
            {
                if (side == PalletThrowZone.ThrowSide.Left)
                {
                    playerInLeftZone = player;
                }
                else if (side == PalletThrowZone.ThrowSide.Right)
                {
                    playerInRightZone = player;
                }
            }
            return;
        }
        
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
    
    public void OnKillerEnterBreakZone(KillerAgent killer, BreakZone.BreakSide side)
    {
        if (!isDropped)
            return;
            
        if (side == BreakZone.BreakSide.Left)
        {
            killerInLeftBreakZone = killer;
            Debug.Log("[PalletController] Killer entered left break zone");
        }
        else if (side == BreakZone.BreakSide.Right)
        {
            killerInRightBreakZone = killer;
            Debug.Log("[PalletController] Killer entered right break zone");
        }
    }
    
    public void OnKillerExitBreakZone(KillerAgent killer, BreakZone.BreakSide side)
    {
        if (side == BreakZone.BreakSide.Left && killerInLeftBreakZone == killer)
        {
            killerInLeftBreakZone = null;
            Debug.Log("[PalletController] Killer exited left break zone");
        }
        else if (side == BreakZone.BreakSide.Right && killerInRightBreakZone == killer)
        {
            killerInRightBreakZone = null;
            Debug.Log("[PalletController] Killer exited right break zone");
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
    
    public bool TryBreakPalletFromInteraction(GameObject interactingObject)
    {
        Debug.Log($"[PalletController] TryBreakPallet called, isDropped: {isDropped}");
        
        if (!isDropped)
        {
            Debug.Log("[PalletController] Pallet not dropped, cannot break");
            return false;
        }
            
        KillerAgent killer = interactingObject.GetComponent<KillerAgent>();
        if (killer == null)
        {
            Debug.Log("[PalletController] Not a killer agent");
            return false;
        }
        
        Debug.Log($"[PalletController] Killer found, checking zones. Left: {killerInLeftBreakZone != null}, Right: {killerInRightBreakZone != null}");
        
        // Check if killer is in either break zone
        Transform breakPosition = null;
        if (killer == killerInLeftBreakZone && leftDropPosition != null)
        {
            breakPosition = leftDropPosition;
            Debug.Log("[PalletController] Breaking from left position");
        }
        else if (killer == killerInRightBreakZone && rightDropPosition != null)
        {
            breakPosition = rightDropPosition;
            Debug.Log("[PalletController] Breaking from right position");
        }
        
        if (breakPosition != null)
        {
            // Start pallet breaking coroutine
            killer.StartCoroutine(killer.PerformPalletBreak(this, breakPosition));
            return true;
        }
        
        Debug.Log("[PalletController] No valid break position found");
        return false;
    }
    
    public void DestroyPallet()
    {
        // Destroy the entire pallet GameObject (parent of parent of hitboxes)
        // The hitboxes are children of droppedObject, which is child of this GameObject
        Destroy(gameObject);
    }
    
    public bool TryDropPalletFromInteraction(GameObject interactingObject)
    {
        // Only survivors can drop pallets
        MonoBehaviour player = interactingObject.GetComponent<SurvivorAgent>();
        if (player == null)
        {
            player = interactingObject.GetComponent<PlayerController>();
        }
        
        // Don't allow killer to drop pallets
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
            // Check if survivor is in chase and penalize if not
            SurvivorAgent survivor = playerToMove.GetComponent<SurvivorAgent>();
            if (survivor != null)
            {
                // Find the chase manager (on the killer in this environment)
                Transform envRoot = survivor.transform.parent;
                if (envRoot != null)
                {
                    KillerAgent killer = envRoot.GetComponentInChildren<KillerAgent>();
                    if (killer != null)
                    {
                        ChaseManager chaseManager = killer.GetComponent<ChaseManager>();
                        if (chaseManager != null)
                        {
                            bool inChase = chaseManager.IsInChase(survivor);
                            if (!inChase)
                            {
                                // Penalize survivor for dropping pallet outside of chase
                                survivor.AddReward(-0.5f);
                                Debug.Log($"[PalletController] Penalized {survivor.name} for dropping pallet out of chase");
                            }
                            else
                            {
                                Debug.Log($"[PalletController] {survivor.name} is in chase, no penalty");
                            }
                        }
                    }
                }
            }
            
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
        
        // Check if killer is in stun range when pallet drops
        KillerAgent stunnedKiller = CheckForKillerStun(endPosition);
        SurvivorAgent survivor = player.GetComponent<SurvivorAgent>();
        
        if (standingObject != null)
            standingObject.SetActive(false);
        
        if (droppedObject != null)
            droppedObject.SetActive(true);
        
        // If killer was stunned, start stun coroutine
        if (stunnedKiller != null)
        {
            Debug.Log($"[PalletController] Killer stunned by pallet drop!");
            
            // Reward survivor for stun
            if (survivor != null)
            {
                survivor.RewardPalletStun();
            }
            
            // Penalize killer for being stunned
            stunnedKiller.RewardPalletStunned();
            
            // Start killer stun
            StartCoroutine(StunKiller(stunnedKiller));
        }
        
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
    
    private KillerAgent CheckForKillerStun(Vector3 dropPosition)
    {
        // Check for killer in stun radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(dropPosition, stunCheckRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Killer"))
            {
                KillerAgent killer = col.GetComponent<KillerAgent>();
                if (killer != null)
                {
                    return killer;
                }
            }
        }
        return null;
    }
    
    private IEnumerator StunKiller(KillerAgent killer)
    {
        // Lock the killer
        InteractionController.LockCharacter(killer);
        
        // Stop killer movement
        Rigidbody2D killerRb = killer.GetComponent<Rigidbody2D>();
        if (killerRb != null)
        {
            killerRb.linearVelocity = Vector2.zero;
        }
        
        float elapsed = 0f;
        while (elapsed < stunDuration)
        {
            // Keep killer frozen
            if (killerRb != null)
            {
                killerRb.linearVelocity = Vector2.zero;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Unlock the killer
        InteractionController.UnlockCharacter(killer);
    }
}
