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
    public List<TickData> StartStep(Vector3Int playerInput)
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
        Vector3Int moveVector = playerInput;
        Vector3Int targetPos = player.position + moveVector;
        
        // Check if move is valid (not into a wall)
        if (!IsValidMove(targetPos, moveVector))
        {
            return null;
        }
        
        // Initialize step
        isProcessingStep = true;
        tickCount = 0;
        currentStepData.Clear();
        
        // Execute player movement
        TickData firstTick = new TickData(tickCount);
        ExecutePlayerMove(player, moveVector, targetPos, firstTick);
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
    private void ExecutePlayerMove(Object player, Vector3Int direction, Vector3Int targetPos, TickData tickData)
    {
        Vector3Int abovePlayerOldPos = player.position + Vector3Int.up;
        Object carriedObject = gameState.GetObjectAt(abovePlayerOldPos);
        
        Object targetObject = gameState.GetObjectAt(targetPos);
        bool playerMoved = false;
        
        if (targetObject != null)
        {
            Vector3Int pushTarget = targetPos + direction;
            
            if (IsValidMove(pushTarget, direction))
            {
                PushObject(targetObject, direction, tickData);
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
    private void MoveCarriedObjectDirect(Object carriedObject, Vector3Int direction, TickData tickData)
    {
        Vector3Int aboveCarriedMain = carriedObject.position + Vector3Int.up;
        Object nextCarriedMain = gameState.GetObjectAt(aboveCarriedMain);

        Object nextCarriedSecondary = null;
        if (carriedObject.type == "1x2_box")
        {
            Vector3Int secondaryPos = carriedObject.GetSecondaryPosition();
            Vector3Int aboveCarriedSecondary = secondaryPos + Vector3Int.up;
            nextCarriedSecondary = gameState.GetObjectAt(aboveCarriedSecondary);
        }

        if (nextCarriedSecondary == nextCarriedMain)
        {
            nextCarriedSecondary = null;
        }

        Vector3Int carriedTarget = carriedObject.position + direction;
        bool canMove = IsValidMove(carriedTarget, direction) && IsCellFreeOrSelf(carriedTarget, carriedObject);

        if (carriedObject.type == "1x2_box")
        {
            Vector3Int secondaryPos = carriedObject.GetSecondaryPosition();
            Vector3Int secondaryTarget = secondaryPos + direction;
            if (carriedObject.position != secondaryPos)
            {
                canMove = canMove
                      && IsValidMove(secondaryTarget, direction)
                      && IsCellFreeOrSelf(secondaryTarget, carriedObject);
            }
        }

        if (canMove)
        {
            MoveObject(carriedObject, carriedTarget, TaskAction.Move, tickData);

            if (nextCarriedMain != null && nextCarriedMain.IsAlive())
            {
                MoveCarriedObjectDirect(nextCarriedMain, direction, tickData);
            }

            if (nextCarriedSecondary != null && nextCarriedSecondary.IsAlive())
            {
                MoveCarriedObjectDirect(nextCarriedSecondary, direction, tickData);
            }
        }
    }

    private bool IsCellFreeOrSelf(Vector3Int pos, Object self)
    {
        Object objAtPos = gameState.GetObjectAt(pos);
        return objAtPos == null || objAtPos == self || !objAtPos.IsAlive();
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
                else
                {
                    bool canFall = IsValidMove(belowPos, Vector3Int.down) && gameState.GetObjectAt(belowPos) == null;

                    if (obj.type == "1x2_box")
                    {
                        Vector3Int secondaryPos = obj.GetSecondaryPosition();
                        Vector3Int secondaryBelow = secondaryPos + Vector3Int.down;
                        canFall = canFall
                                  && IsValidMove(secondaryBelow, Vector3Int.down)
                                  && gameState.GetObjectAt(secondaryBelow) == null;
                    }

                    if (canFall)
                    {
                        MoveObject(obj, belowPos, TaskAction.Fall, tickData);
                        anyFalling = true;
                    }
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
            if (!obj.IsAlive() && (obj.type == "box" || obj.type == "player" || obj.type == "1x2_box"))
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
    private bool IsValidMove(Vector3Int pos, Vector3Int direction)
    {
        if (direction == Vector3Int.zero)
        {
            return false;
        }

        GeoType geoType = geoState.GetGeoTypeAt(pos);
        if (geoType == GeoType.Block)
        {
            return false;
        }

        Object objAtPos = gameState.GetObjectAt(pos);
        if (objAtPos == null || !objAtPos.IsAlive())
        {
            return true;
        }

        Vector3Int nextPos = pos + direction;
        if (objAtPos.type == "1x2_box")
        {
            if (pos == objAtPos.position && direction == objAtPos.rotation)
            {
                nextPos = objAtPos.GetSecondaryPosition() + direction;
            }
            else if (pos == objAtPos.GetSecondaryPosition()&& direction == -objAtPos.rotation)
            {
                nextPos = objAtPos.position + direction;
            }
        }
        return IsValidMove(nextPos, direction);
    }

    /// <summary>
    /// Push an object in a direction
    /// </summary>
    private void PushObject(Object obj, Vector3Int direction, TickData tickData)
    {
        Vector3Int aboveObjOldPos1 = obj.position + Vector3Int.up;
        Object carriedByPushed1 = gameState.GetObjectAt(aboveObjOldPos1);

        Object carriedByPushed2 = null;
        if (obj.type == "1x2_box")
        {
            Vector3Int aboveObjOldPos2 = obj.GetSecondaryPosition() + Vector3Int.up;
            carriedByPushed2 = gameState.GetObjectAt(aboveObjOldPos2);
        }
        
        if (obj.type == "1x2_box" && carriedByPushed2 == carriedByPushed1)
        {
            carriedByPushed2 = null;
        }

        Vector3Int targetPos = obj.position + direction;

        MoveObject(obj, targetPos, TaskAction.Slide, tickData);
        
        if (carriedByPushed1 != null && carriedByPushed1.IsAlive())
        {
            MoveCarriedObjectDirect(carriedByPushed1, direction, tickData);
        }

        if (obj.type == "1x2_box" && carriedByPushed2 != null && carriedByPushed2.IsAlive())
        {
            MoveCarriedObjectDirect(carriedByPushed2, direction, tickData);
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
        
        Vector3Int moveDir = targetPos - fromPos;
        Task task = new Task(obj.color, actionType, moveDir);
        tickData.tasks.Add(task);
    }
    
    /// <summary>
    /// Kill an object and record it
    /// </summary>
    private void KillObject(Object obj, TickData tickData)
    {
        obj.alive = false;
        
        Task task = new Task(obj.color, TaskAction.Die, Vector3Int.zero);
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
        
        Task task = new Task(obj.color, TaskAction.Respawn, Vector3Int.zero);
        tickData.tasks.Add(task);
        
        ObjectMovement movement = new ObjectMovement(obj, oldPos, spawnPos, TaskAction.Respawn);
        tickData.movements.Add(movement);
    }
}
