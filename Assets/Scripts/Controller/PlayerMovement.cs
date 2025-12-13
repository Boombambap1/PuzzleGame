using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.1f;
    
    private GamePhysics gamePhysics;
    private GameState gameState;
    private Object playerObject;
    private float lastInputTime;
    private bool isProcessingStep = false;
    
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
        
        Debug.Log($"Player registered at position: {gridPos}");
    }
    
    void Update()
    {
        // Don't accept input if player not initialized
        if (playerObject == null || !playerObject.IsAlive())
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
        Debug.Log($"[PlayerInput] Processing input: {direction} from position {playerObject.position}");
        
        // Call the physics system
        List<TickData> stepData = gamePhysics.StartStep(direction);
        
        if (stepData != null && stepData.Count > 0)
        {
            Debug.Log($"[PlayerInput] Step completed with {stepData.Count} ticks");
            
            // Update visual position to match game state
            transform.position = playerObject.position;
            
            // Log what happened
            foreach (TickData tick in stepData)
            {
                Debug.Log($"  {tick}");
                foreach (ObjectMovement movement in tick.movements)
                {
                    Debug.Log($"    {movement}");
                }
            }
        }
        else
        {
            Debug.Log("[PlayerInput] Movement blocked or invalid (stepData was null)");
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
            transform.position = playerObject.position;
        }
    }
}
