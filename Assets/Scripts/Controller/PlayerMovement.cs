using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.1f;
    
    [Header("Animation")]
    public float moveSpeed = 5f; // Speed of smooth movement
    
    private GamePhysics gamePhysics;
    private GameState gameState;
    private Object playerObject;
    private float lastInputTime;
    private bool isProcessingStep = false;
    
    // For smooth movement
    private Vector3 visualPosition;
    private bool isAnimating = false;
    
    void Start()
    {
        // Find the game systems
        gamePhysics = FindObjectOfType<GamePhysics>();
        gameState = FindObjectOfType<GameState>();
        
        if (gamePhysics == null)
        {
            Debug.LogError("PlayerInput: GamePhysics not found in scene!");
            return;
        }
        
        if (gameState == null)
        {
            Debug.LogError("PlayerInput: GameState not found in scene!");
            return;
        }
        
        // Register this player with the game state
        RegisterPlayer();
        
        lastInputTime = -inputCooldown;
    }
    
    
    /// <summary>
    /// Register this GameObject as the player in the game state
    /// </summary>
    private void RegisterPlayer()
    {
        // Get the grid position from this GameObject's world position
        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);
        
        // Create the player Object
        playerObject = new Object("none", "robot", gridPos, Direction.Forward);
        
        // Register with game state
        gameState.PlaceObjectAt(playerObject, gridPos);
        
        // Initialize visual position
        visualPosition = transform.position;
        
        Debug.Log($"Player registered at position: {gridPos}");
    }
    
    void Update()
    {
        // FIRST: Handle smooth animation
        if (playerObject != null)
        {
            Vector3 targetPos = playerObject.position;
            float distance = Vector3.Distance(visualPosition, targetPos);
            
            if (distance > 0.01f)
            {
                // Animate towards target
                float step = moveSpeed * Time.deltaTime;
                Vector3 oldVisualPos = visualPosition;
                visualPosition = Vector3.MoveTowards(visualPosition, targetPos, step);
                transform.position = visualPosition;
                isAnimating = true;
                
                Debug.Log($"[Animation] Frame {Time.frameCount}: {oldVisualPos:F3} -> {visualPosition:F3} (step: {step:F3}, dist remaining: {distance:F3})");
            }
            else
            {
                // Snap to final position
                visualPosition = targetPos;
                transform.position = targetPos;
                
                if (isAnimating)
                {
                    Debug.Log($"[Animation] Complete at frame {Time.frameCount}!");
                    isAnimating = false;
                }
            }
        }
        
        // SECOND: Handle input (only if not animating)
        
        // Don't accept input if player not initialized
        if (playerObject == null || !playerObject.IsAlive())
        {
            return;
        }
        
        // Don't accept input while animating
        if (isAnimating)
        {
            return;
        }
        
        // Don't accept input if we're still processing a step
        if (isProcessingStep)
        {
            return;
        }
        
        // Check input cooldown
        if (Time.time - lastInputTime < inputCooldown)
        {
            return;
        }
        
        // Check for movement input
        Direction inputDirection = Direction.None;
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            inputDirection = Direction.Forward;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            inputDirection = Direction.Backward;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            inputDirection = Direction.Left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            inputDirection = Direction.Right;
        }
        
        // If valid input, process movement
        if (inputDirection != Direction.None)
        {
            ProcessMovement(inputDirection);
            lastInputTime = Time.time;
        }
    }
    
    private void ProcessMovement(Direction direction)
    {
        Vector3 visualPosBefore = visualPosition;
        
        Debug.Log($"[PlayerInput] Processing input: {direction} from position {playerObject.position}");
        Debug.Log($"[PlayerInput] Visual position before step: {visualPosition}");
        
        // Block further input
        isProcessingStep = true;
        
        // Call the physics system
        List<TickData> stepData = gamePhysics.StartStep(direction);
        
        if (stepData != null && stepData.Count > 0)
        {
            Debug.Log($"[PlayerInput] Step completed with {stepData.Count} ticks");
            Debug.Log($"[PlayerInput] Player now at: {playerObject.position}, visual still at: {visualPosition}");
            
            // Check if visualPosition changed during physics
            if (visualPosition != visualPosBefore)
            {
                Debug.LogError($"[PlayerInput] WARNING: visualPosition changed during physics! Was {visualPosBefore}, now {visualPosition}");
            }
            
            // Log what happened
            foreach (TickData tick in stepData)
            {
                Debug.Log($"  {tick}");
                foreach (ObjectMovement movement in tick.movements)
                {
                    Debug.Log($"    {movement}");
                }
            }
            
            // Animation will start in Update()
            isProcessingStep = false;
        }
        else
        {
            Debug.Log("[PlayerInput] Movement blocked or invalid (stepData was null)");
            isProcessingStep = false;
        }
    }
    
    /// <summary>
    /// Sync the GameObject's visual position with the game state
    /// Call this after physics updates
    /// </summary>
    public void UpdateVisualPosition()
    {
        if (playerObject != null)
        {
            visualPosition = playerObject.position;
            transform.position = visualPosition;
        }
    }
}