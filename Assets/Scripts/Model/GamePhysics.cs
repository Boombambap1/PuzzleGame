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
        // Apply initial gravity after all objects register
        Invoke("ApplyInitialGravity", 0.1f);
    }
    
    /// <summary>
    /// Apply initial gravity when scene loads
    /// </summary>
    private void ApplyInitialGravity()
    {
        Debug.Log("[GamePhysics] Applying initial gravity...");
        ApplyGravity();
    }
    
    /// <summary>
    /// Apply gravity to all objects until they reach stable ground
    /// </summary>
    public void ApplyGravity()
    {
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
    }
    
    /// <summary>
    /// Initiates a step based on player input direction
    /// </summary>
    public List<TickData> StartStep(Direction playerInput)
    {
        // Don't start a new step if one is already processing
        if (isProcessingStep)
        {
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
            return null;
        }
        
        // Calculate target position
        Vector3Int moveVector = DirectionToVector(playerInput);
        Vector3Int targetPos = player.position + moveVector;
        
        // Check if move is valid (not into a wall or void)
        if (!IsValidMove(targetPos))
        {
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
                    break;
                }
            }
        }
        
        if (tickCount >= maxTicks)
        {
            Debug.LogWarning("[Step] Max tick limit reached - possible softlock detected");
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
        // FIRST: Check what the player is carrying BEFORE moving
        Vector3Int abovePlayerOldPos = player.position + Vector3Int.up;
        Object carriedObject = gameState.GetObjectAt(abovePlayerOldPos);
        
        Debug.Log($"[ExecutePlayerMove] Player at {player.position}, checking for carried object at {abovePlayerOldPos}");
        if (carriedObject != null)
        {
            Debug.Log($"[ExecutePlayerMove] Found carried object: {carriedObject.type} at {carriedObject.position}");
        }
        
        // Check if we're pushing a box in front
        Object targetObject = gameState.GetObjectAt(targetPos);
        
        bool playerMoved = false;
        
        if (targetObject != null)
        {
            // Try to push the object
            Vector3Int pushTarget = targetPos + DirectionToVector(direction);
            
            if (CanPushObject(targetObject, pushTarget))
            {
                // Push the object (and anything it's carrying)
                PushObject(targetObject, direction, pushTarget, tickData);
                
                // Move player
                MoveObject(player, targetPos, TaskAction.Move, tickData);
                playerMoved = true;
            }
            // If can't push, player doesn't move
        }
        else
        {
            // Empty space in front, player can move
            MoveObject(player, targetPos, TaskAction.Move, tickData);
            playerMoved = true;
        }
        
        // AFTER player moved (or tried to move), handle the carried object
        // The carried object tries to follow ONLY if the player actually moved
        if (playerMoved && carriedObject != null && carriedObject.IsAlive())
        {
            Debug.Log($"[ExecutePlayerMove] Player moved, now trying to move carried object");
            MoveCarriedObjectDirect(carriedObject, direction, tickData);
        }
    }
    
    /// <summary>
    /// Move a specific carried object (after the carrier has already moved)
    /// </summary>
    private void MoveCarriedObjectDirect(Object carriedObject, Direction direction, TickData tickData)
    {
        Debug.Log($"[MoveCarriedDirect] Moving {carriedObject.type} from {carriedObject.position}");
        
        // Calculate target position for carried object (same direction as carrier moved)
        Vector3Int carriedTarget = carriedObject.position + DirectionToVector(direction);
        
        Debug.Log($"[MoveCarriedDirect] Target: {carriedTarget}");
        
        // Check if the carried object can move there
        if (CanMoveCarriedObject(carriedObject, carriedTarget))
        {
            Debug.Log($"[MoveCarriedDirect] Move successful");
            
            // Move the carried object
            MoveObject(carriedObject, carriedTarget, TaskAction.Move, tickData);
            
            // Recursively check if THIS object is carrying something
            Vector3Int aboveCarried = carriedObject.position + Vector3Int.up;
            Object nextCarried = gameState.GetObjectAt(aboveCarried);
            
            if (nextCarried != null && nextCarried.IsAlive())
            {
                Debug.Log($"[MoveCarriedDirect] Found stacked object, moving it too");
                MoveCarriedObjectDirect(nextCarried, direction, tickData);
            }
        }
        else
        {
            Debug.Log($"[MoveCarriedDirect] Blocked - object stays at {carriedObject.position}");
            // Object stays where it is - will fall next tick if no support
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
            bool inFreefall = gameState.IsObjectInFreefall(obj);
            
            if (inFreefall)
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
    /// Check if a position is valid for movement (not blocked by a wall)
    /// </summary>
    private bool IsValidMove(Vector3Int pos)
    {
        GeoType geoType = geoState.GetGeoTypeAt(pos);
        // Can move into Void (empty space), Spawn, or Exit
        // Cannot move into Block (solid wall)
        return geoType != GeoType.Block;
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
    /// Check if a carried object can move to target position
    /// Similar to CanPushObject but for carried objects
    /// </summary>
    private bool CanMoveCarriedObject(Object obj, Vector3Int targetPos)
    {
        // Check if target has a solid block (GeoBlock)
        GeoType geoAtTarget = geoState.GetGeoTypeAt(targetPos);
        if (geoAtTarget == GeoType.Block)
        {
            Debug.Log($"[CanMoveCarried] Blocked by GeoBlock at {targetPos}");
            return false;
        }
        
        // Check if there's another object at target
        Object objectAtTarget = gameState.GetObjectAt(targetPos);
        if (objectAtTarget != null && objectAtTarget.IsAlive())
        {
            Debug.Log($"[CanMoveCarried] Blocked by {objectAtTarget.type} at {targetPos}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Push an object in a direction
    /// </summary>
    private void PushObject(Object obj, Direction direction, Vector3Int targetPos, TickData tickData)
    {
        // Check what this object is carrying BEFORE moving
        Vector3Int aboveObjOldPos = obj.position + Vector3Int.up;
        Object carriedByPushed = gameState.GetObjectAt(aboveObjOldPos);
        
        // Move the pushed object
        MoveObject(obj, targetPos, TaskAction.Slide, tickData);
        
        // Move anything it was carrying
        if (carriedByPushed != null && carriedByPushed.IsAlive())
        {
            MoveCarriedObjectDirect(carriedByPushed, direction, tickData);
        }
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
            Direction.Forward => new Vector3Int(0, 0, 1),  // Z+1
            Direction.Backward => new Vector3Int(0, 0, -1), // Z-1
            Direction.Left => new Vector3Int(-1, 0, 0),     // X-1
            Direction.Right => new Vector3Int(1, 0, 0),     // X+1
            Direction.Up => new Vector3Int(0, 1, 0),        // Y+1
            Direction.Down => new Vector3Int(0, -1, 0),     // Y-1
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
