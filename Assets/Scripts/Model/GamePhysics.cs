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
        
        // Finish step
        isProcessingStep = false;
        
        // Check win conditions after step completes
        CheckWinCondition();
        
        return new List<TickData>(currentStepData);
    }
    
    /// <summary>
    /// Check if win conditions are met and trigger win event
    /// </summary>
    private void CheckWinCondition()
    {
        if (gameState.CheckWinConditions())
        {
            Debug.Log("========================================");
            Debug.Log("===== LEVEL COMPLETE! =====");
            Debug.Log("========================================");
            OnLevelComplete();
        }
    }
    
    /// <summary>
    /// Called when level is completed
    /// </summary>
    private void OnLevelComplete()
    {
        SendMessage("OnWinConditionMet", SendMessageOptions.DontRequireReceiver);
    }
    
    /// <summary>
    /// Continues stepping until stable state is reached
    /// </summary>
    private void Step()
    {
        int maxTicks = 100;
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
                if (consecutiveEmptyTicks >= 1)
                {
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Processes a single tick - returns true if any movement occurred
    /// </summary>
    private bool Tick(TickData tickData)
    {
        bool anyMovement = false;
        
        anyMovement |= ProcessBoxSliding(tickData);
        anyMovement |= ProcessFalling(tickData);
        anyMovement |= ProcessRespawning(tickData);
        
        return anyMovement;
    }
    
    /// <summary>
    /// Executes the initial player movement
    /// </summary>
    private void ExecutePlayerMove(Object player, Direction direction, Vector3Int targetPos, TickData tickData)
    {
        Vector3Int abovePlayerOldPos = player.position + Vector3Int.up;
        Object carriedObject = gameState.GetObjectAt(abovePlayerOldPos);
        
        Object targetObject = gameState.GetObjectAt(targetPos);
        bool playerMoved = false;
        
        if (targetObject != null)
        {
            Vector3Int pushTarget = targetPos + DirectionToVector(direction);
            
            if (CanPushObject(targetObject, pushTarget))
            {
                PushObject(targetObject, direction, pushTarget, tickData);
                MoveObject(player, targetPos, TaskAction.Move, tickData);
                playerMoved = true;
            }
        }
        else
        {
            MoveObject(player, targetPos, TaskAction.Move, tickData);
            playerMoved = true;
        }
        
        if (playerMoved && carriedObject != null && carriedObject.IsAlive())
        {
            MoveCarriedObjectDirect(carriedObject, direction, tickData);
        }
    }
    
    /// <summary>
    /// Move a specific carried object (after the carrier has already moved)
    /// </summary>
    private void MoveCarriedObjectDirect(Object carriedObject, Direction direction, TickData tickData)
    {
        Vector3Int aboveCarriedOldPos = carriedObject.position + Vector3Int.up;
        Object nextCarried = gameState.GetObjectAt(aboveCarriedOldPos);
        
        Vector3Int carriedTarget = carriedObject.position + DirectionToVector(direction);
        
        if (CanMoveCarriedObject(carriedObject, carriedTarget))
        {
            MoveObject(carriedObject, carriedTarget, TaskAction.Move, tickData);
            
            if (nextCarried != null && nextCarried.IsAlive())
            {
                MoveCarriedObjectDirect(nextCarried, direction, tickData);
            }
        }
    }
    
    /// <summary>
    /// Process boxes that are sliding from being pushed
    /// </summary>
    private bool ProcessBoxSliding(TickData tickData)
    {
        bool anySliding = false;
        List<Object> allObjects = gameState.GetAllObjects();
        
        foreach (Object obj in allObjects)
        {
            if (obj.type == "box" && obj.IsAlive())
            {
                // Future: sliding mechanics
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
            
            bool inFreefall = gameState.IsObjectInFreefall(obj);
            
            if (inFreefall)
            {
                Vector3Int belowPos = obj.position + Vector3Int.down;
                
                if (obj.position.y <= DEATH_BOUND)
                {
                    KillObject(obj, tickData);
                    anyFalling = true;
                }
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
        
        Debug.Log($"[ProcessRespawning] Checking {allObjects.Count} objects");
        
        foreach (Object obj in allObjects)
        {
            Debug.Log($"[Respawn Check] {obj.color} {obj.type}, alive: {obj.alive}, prefab: {(obj.prefab != null ? obj.prefab.name : "NULL")}");
            
            // Check if object is dead and can respawn (boxes or player)
            if (!obj.IsAlive() && (obj.type == "box" || obj.type == "robot"))
            {
                if (obj.prefab == null)
                {
                    Debug.LogWarning($"[Respawn] {obj.type} has no prefab reference!");
                    continue;
                }
                
                Vector3Int? spawnPos = geoState.FindSpawnPosition(obj.prefab);
                
                if (spawnPos.HasValue)
                {
                    Vector3Int respawnPosition = new Vector3Int(
                        spawnPos.Value.x, 
                        spawnPos.Value.y + RESPAWN_HEIGHT, 
                        spawnPos.Value.z
                    );
                    
                    Debug.Log($"[Respawn] Respawning {obj.type} at {respawnPosition}");
                    RespawnObject(obj, respawnPosition, tickData);
                    anyRespawning = true;
                }
                else
                {
                    Debug.LogWarning($"[Respawn] No spawn point for prefab '{obj.prefab.name}'");
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
        return geoType != GeoType.Block;
    }
    
    /// <summary>
    /// Check if an object can be pushed to a target position
    /// </summary>
    private bool CanPushObject(Object obj, Vector3Int targetPos)
    {
        if (!IsValidMove(targetPos)) return false;
        if (gameState.GetObjectAt(targetPos) != null) return false;
        return true;
    }
    
    /// <summary>
    /// Check if a carried object can move to target position
    /// </summary>
    private bool CanMoveCarriedObject(Object obj, Vector3Int targetPos)
    {
        GeoType geoAtTarget = geoState.GetGeoTypeAt(targetPos);
        if (geoAtTarget == GeoType.Block)
        {
            return false;
        }
        
        Object objectAtTarget = gameState.GetObjectAt(targetPos);
        if (objectAtTarget != null && objectAtTarget.IsAlive())
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Push an object in a direction
    /// </summary>
    private void PushObject(Object obj, Direction direction, Vector3Int targetPos, TickData tickData)
    {
        Vector3Int aboveObjOldPos = obj.position + Vector3Int.up;
        Object carriedByPushed = gameState.GetObjectAt(aboveObjOldPos);
        
        MoveObject(obj, targetPos, TaskAction.Slide, tickData);
        
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
        
        gameState.MoveObjectTo(obj, targetPos);
        
        ObjectMovement movement = new ObjectMovement(obj, fromPos, targetPos, actionType);
        tickData.movements.Add(movement);
        
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
            Direction.Forward => new Vector3Int(0, 0, 1),
            Direction.Backward => new Vector3Int(0, 0, -1),
            Direction.Left => new Vector3Int(-1, 0, 0),
            Direction.Right => new Vector3Int(1, 0, 0),
            Direction.Up => new Vector3Int(0, 1, 0),
            Direction.Down => new Vector3Int(0, -1, 0),
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
