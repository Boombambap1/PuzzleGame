using System.Collections.Generic;
using UnityEngine;

public class GamePhysics : MonoBehaviour
{
    // Constants
    private const int DEATH_BOUND = -10;
    private const int RESPAWN_HEIGHT = 10;
    private const int MOVEMENT_DISTANCE = 1;
    
    // References
    private GeoState geoState;
    private GameState gameState;
    
    // Step tracking
    private int tickCount;
    private bool isProcessingStep;
    private List<TickData> currentStepData;
    
    void Awake()
    {
        geoState = GetComponent<GeoState>();
        gameState = GetComponent<GameState>();
        currentStepData = new List<TickData>();
    }
    
    void Start()
    {
        // Apply initial gravity when scene loads
        ApplyGravity();
    }
    
    /// <summary>
    /// Apply gravity to all objects until they reach stable ground
    /// </summary>
    public void ApplyGravity()
    {
        Debug.Log("Applying gravity...");
        isProcessingStep = true;
        tickCount = 0;
        currentStepData.Clear();
        
        // Keep processing until nothing is falling
        bool objectsFalling = true;
        int maxIterations = 50;
        int iteration = 0;
        
        while (objectsFalling && iteration < maxIterations)
        {
            iteration++;
            tickCount++;
            TickData tickData = new TickData(tickCount);
            
            objectsFalling = ProcessFalling(tickData);
            
            if (objectsFalling)
            {
                currentStepData.Add(tickData);
            }
        }
        
        isProcessingStep = false;
        Debug.Log($"Gravity applied - {currentStepData.Count} ticks of falling");
    }
    
    /// <summary>
    /// Initiates a step based on player input direction
    /// </summary>
    public List<TickData> StartStep(Direction playerInput)
    {
        // Don't start a new step if one is already processing
        if (isProcessingStep)
        {
            Debug.Log("[GamePhysics] Step already in progress, ignoring input");
            return null;
        }
        
        // Get player object
        Object player = gameState.GetPlayer();
        if (player == null)
        {
            Debug.LogError("[GamePhysics] Player not found!");
            return null;
        }
        
        if (!player.IsAlive())
        {
            Debug.Log("[GamePhysics] Player is dead");
            return null;
        }
        
        Debug.Log($"[GamePhysics] Starting step - Player at {player.position}, moving {playerInput}");
        
        // Calculate target position
        Vector3Int targetPos = player.position + DirectionToVector(playerInput);
        Debug.Log($"[GamePhysics] Target position: {targetPos}");
        
        // Check if move is valid (not into a wall or void)
        if (!IsValidMove(targetPos))
        {
            GeoType geoAtTarget = geoState.GetGeoTypeAt(targetPos);
            Debug.Log($"[GamePhysics] Invalid move - geo at target is {geoAtTarget}");
            return null;
        }
        
        // Initialize step
        isProcessingStep = true;
        tickCount = 0;
        currentStepData.Clear();
        
        // Execute player movement
        TickData firstTick = new TickData(tickCount);
        ExecutePlayerMove(player, playerInput, targetPos, firstTick);
        currentStepData.Add(firstTick);
        
        // Process the rest of the step
        Step();
        
        // Finish and return step data
        isProcessingStep = false;
        
        Debug.Log($"Step completed with {currentStepData.Count} ticks");
        return new List<TickData>(currentStepData);
    }
    
    /// <summary>
    /// Continues stepping until stable state is reached
    /// </summary>
    private void Step()
    {
        int maxTicks = 100; // Safety limit to prevent infinite loops
        int consecutiveEmptyTicks = 0;
        
        while (tickCount < maxTicks)
        {
            tickCount++;
            TickData tickData = new TickData(tickCount);
            
            bool movementOccurred = Tick(tickData);
            
            if (movementOccurred)
            {
                currentStepData.Add(tickData);
                consecutiveEmptyTicks = 0;
            }
            else
            {
                consecutiveEmptyTicks++;
                // Stable state reached - nothing moved for a full tick
                if (consecutiveEmptyTicks >= 1)
                {
                    Debug.Log($"Stable state reached at tick {tickCount}");
                    break;
                }
            }
        }
        
        if (tickCount >= maxTicks)
        {
            Debug.LogWarning("Max tick limit reached - possible softlock detected");
        }
    }
    
    /// <summary>
    /// Processes a single tick - returns true if any movement occurred
    /// </summary>
    private bool Tick(TickData tickData)
    {
        bool anyMovement = false;
        
        // 1. Process box sliding (from being pushed)
        anyMovement |= ProcessBoxSliding(tickData);
        
        // 2. Process falling for all objects
        anyMovement |= ProcessFalling(tickData);
        
        // 3. Process respawning
        anyMovement |= ProcessRespawning(tickData);
        
        return anyMovement;
    }
    
    /// <summary>
    /// Executes the initial player movement
    /// </summary>
    private void ExecutePlayerMove(Object player, Direction direction, Vector3Int targetPos, TickData tickData)
    {
        // Check if we're pushing a box
        Object targetObject = gameState.GetObjectAt(targetPos);
        
        if (targetObject != null)
        {
            // Try to push the object
            Vector3Int pushTarget = targetPos + DirectionToVector(direction);
            
            if (CanPushObject(targetObject, pushTarget))
            {
                // Push the object
                PushObject(targetObject, direction, pushTarget, tickData);
                
                // Move player
                MoveObject(player, targetPos, TaskAction.Move, tickData);
            }
            // If can't push, player doesn't move
        }
        else
        {
            // Empty space, just move
            MoveObject(player, targetPos, TaskAction.Move, tickData);
        }
    }
    
    /// <summary>
    /// Process boxes that are sliding from being pushed
    /// </summary>
    private bool ProcessBoxSliding(TickData tickData)
    {
        bool anySliding = false;
        List<Object> allObjects = gameState.GetAllObjects();
        
        // Process each box
        foreach (Object obj in allObjects)
        {
            if (obj.type == "box" && obj.IsAlive())
            {
                // Check if box has momentum (was pushed last tick)
                // For now, boxes only move when initially pushed, not continuously
                // You can extend this for sliding mechanics later
            }
        }
        
        return anySliding;
    }
    
    /// <summary>
    /// Process falling for all objects
    /// </summary>
    private bool ProcessFalling(TickData tickData)
    {
        bool anyFalling = false;
        List<Object> allObjects = gameState.GetAllObjects();
        
        foreach (Object obj in allObjects)
        {
            if (!obj.IsAlive()) continue;
            
            // Check if object should fall
            if (gameState.IsObjectInFreefall(obj))
            {
                Vector3Int belowPos = obj.position + Vector3Int.down;
                
                // Check if below death bound
                if (obj.position.y <= DEATH_BOUND)
                {
                    KillObject(obj, tickData);
                    anyFalling = true;
                }
                // Check if can fall
                else if (IsValidMove(belowPos) && gameState.GetObjectAt(belowPos) == null)
                {
                    MoveObject(obj, belowPos, TaskAction.Fall, tickData);
                    anyFalling = true;
                }
            }
        }
        
        return anyFalling;
    }
    
    /// <summary>
    /// Process respawning for dead objects that can respawn
    /// </summary>
    private bool ProcessRespawning(TickData tickData)
    {
        bool anyRespawning = false;
        List<Object> allObjects = gameState.GetAllObjects();
        
        foreach (Object obj in allObjects)
        {
            if (!obj.IsAlive() && obj.type == "box")
            {
                // Find spawn position for this object's color
                Vector3Int? spawnPos = geoState.FindSpawnPosition(obj.color);
                
                if (spawnPos.HasValue)
                {
                    Vector3Int respawnPosition = new Vector3Int(spawnPos.Value.x, RESPAWN_HEIGHT, spawnPos.Value.z);
                    RespawnObject(obj, respawnPosition, tickData);
                    anyRespawning = true;
                }
            }
        }
        
        return anyRespawning;
    }
    
    /// <summary>
    /// Check if a position is valid for movement
    /// </summary>
    private bool IsValidMove(Vector3Int pos)
    {
        GeoType geoType = geoState.GetGeoTypeAt(pos);
        return geoType != GeoType.Void && geoType != GeoType.Block;
    }
    
    /// <summary>
    /// Check if an object can be pushed to a target position
    /// </summary>
    private bool CanPushObject(Object obj, Vector3Int targetPos)
    {
        // Check if target is valid and empty
        if (!IsValidMove(targetPos)) return false;
        if (gameState.GetObjectAt(targetPos) != null) return false;
        
        return true;
    }
    
    /// <summary>
    /// Push an object in a direction
    /// </summary>
    private void PushObject(Object obj, Direction direction, Vector3Int targetPos, TickData tickData)
    {
        MoveObject(obj, targetPos, TaskAction.Slide, tickData);
    }
    
    /// <summary>
    /// Move an object to a new position and record it
    /// </summary>
    private void MoveObject(Object obj, Vector3Int targetPos, TaskAction actionType, TickData tickData)
    {
        Vector3Int fromPos = obj.position;
        
        // Update game state
        gameState.MoveObjectTo(obj, targetPos);
        
        // Record movement
        ObjectMovement movement = new ObjectMovement(obj, fromPos, targetPos, actionType);
        tickData.movements.Add(movement);
        
        // Record task
        Direction moveDir = VectorToDirection(targetPos - fromPos);
        Task task = new Task(obj.color, actionType, moveDir);
        tickData.tasks.Add(task);
    }
    
    /// <summary>
    /// Kill an object and record it
    /// </summary>
    private void KillObject(Object obj, TickData tickData)
    {
        obj.alive = false;
        
        Task task = new Task(obj.color, TaskAction.Die, Direction.None);
        tickData.tasks.Add(task);
        
        ObjectMovement movement = new ObjectMovement(obj, obj.position, obj.position, TaskAction.Die);
        tickData.movements.Add(movement);
    }
    
    /// <summary>
    /// Respawn an object at a position
    /// </summary>
    private void RespawnObject(Object obj, Vector3Int spawnPos, TickData tickData)
    {
        obj.alive = true;
        Vector3Int oldPos = obj.position;
        gameState.MoveObjectTo(obj, spawnPos);
        
        Task task = new Task(obj.color, TaskAction.Respawn, Direction.None);
        tickData.tasks.Add(task);
        
        ObjectMovement movement = new ObjectMovement(obj, oldPos, spawnPos, TaskAction.Respawn);
        tickData.movements.Add(movement);
    }
    
    /// <summary>
    /// Convert Direction enum to Vector3Int
    /// </summary>
    private Vector3Int DirectionToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Forward => Vector3Int.forward,
            Direction.Backward => Vector3Int.back,
            Direction.Left => Vector3Int.left,
            Direction.Right => Vector3Int.right,
            Direction.Up => Vector3Int.up,
            Direction.Down => Vector3Int.down,
            _ => Vector3Int.zero
        };
    }
    
    /// <summary>
    /// Convert Vector3Int to Direction enum
    /// </summary>
    private Direction VectorToDirection(Vector3Int vec)
    {
        if (vec == Vector3Int.forward) return Direction.Forward;
        if (vec == Vector3Int.back) return Direction.Backward;
        if (vec == Vector3Int.left) return Direction.Left;
        if (vec == Vector3Int.right) return Direction.Right;
        if (vec == Vector3Int.up) return Direction.Up;
        if (vec == Vector3Int.down) return Direction.Down;
        return Direction.None;
    }
}
