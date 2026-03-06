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

    public static event System.Action<List<TickData>> OnStepComplete;

    void Awake()
    {
        geoState = GetComponent<GeoState>();
        gameState = GetComponent<GameState>();
        currentStepData = new List<TickData>();
    }

    void Start()
    {
        Invoke("ApplyInitialGravity", 0.1f);
    }

    private void ApplyInitialGravity()
    {
        ApplyGravity();
    }

    public void ApplyGravity()
    {
        isProcessingStep = true;
        tickCount = 0;
        currentStepData.Clear();

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

    public bool NeedsRespawn()
    {
        foreach (Object obj in gameState.GetAllObjects())
        {
            if (!obj.IsAlive() && obj.prefab != null)
                return true;
        }
        return false;
    }

    public List<TickData> StartStep(Vector3Int playerInput)
    {
        if (isProcessingStep) return null;
        Debug.Log($"[StartStep] input={playerInput}, NeedsRespawn={NeedsRespawn()}");

        Object player = gameState.GetPlayer();
        if (player == null) return null;

        // Handle respawn before anything else
        if (NeedsRespawn())
        {
            isProcessingStep = true;
            tickCount = 0;
            currentStepData.Clear();

            TickData respawnTick = new TickData(tickCount);
            ProcessRespawning(respawnTick);
            currentStepData.Add(respawnTick);

            Step();

            isProcessingStep = false;
            OnStepComplete?.Invoke(new List<TickData>(currentStepData));
            return new List<TickData>(currentStepData);
        }

        if (playerInput == Vector3Int.zero) return null;
        if (!player.IsAlive()) return null;

        Vector3Int moveVector = playerInput;
        Vector3Int targetPos = player.position + moveVector;

        if (!IsValidMove(targetPos, moveVector))
        {
            return null;
        }

        isProcessingStep = true;
        tickCount = 0;
        currentStepData.Clear();

        ResetAllMovements();

        TickData firstTick = new TickData(tickCount);
        ExecutePlayerMove(player, moveVector, targetPos, firstTick);
        currentStepData.Add(firstTick);

        Step();

        isProcessingStep = false;

        CheckWinCondition();

        OnStepComplete?.Invoke(new List<TickData>(currentStepData));
        return new List<TickData>(currentStepData);
    }

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

    private void OnLevelComplete()
    {
        SendMessage("OnWinConditionMet", SendMessageOptions.DontRequireReceiver);
    }

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
                if (consecutiveEmptyTicks >= 2)
                {
                    break;
                }
            }
        }
    }

    private bool Tick(TickData tickData)
    {
        bool anyMovement = false;

        anyMovement |= ProcessBoxSliding(tickData);
        anyMovement |= ProcessFalling(tickData);

        // Flag if anything needs respawning next step
        foreach (Object obj in gameState.GetAllObjects())
        {
            if (!obj.IsAlive() && obj.prefab != null)
            {

            }
        }

        return anyMovement;
    }

    private void ResetAllMovements()
    {
        List<Object> allObjects = gameState.GetAllObjects();
        foreach (Object obj in allObjects)
        {
            obj.movement = Vector3Int.zero;
        }
    }

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

        if (playerMoved && carriedObject != null && carriedObject.IsAlive() && carriedObject.movement == Vector3Int.zero)
        {
            MoveCarriedObjectDirect(carriedObject, direction, tickData);
        }
    }

    private void MoveCarriedObjectDirect(Object carriedObject, Vector3Int direction, TickData tickData)
    {
        if (carriedObject.movement != Vector3Int.zero) return;

        Vector3Int aboveCarriedMain = carriedObject.position + Vector3Int.up;
        Object nextCarriedMain = gameState.GetObjectAt(aboveCarriedMain);

        Object nextCarriedSecondary = null;
        if (carriedObject.type == "1x2_box")
        {
            Vector3Int secondaryPos = carriedObject.GetSecondaryPosition();
            Vector3Int aboveCarriedSecondary = secondaryPos + Vector3Int.up;
            nextCarriedSecondary = gameState.GetObjectAt(aboveCarriedSecondary);
        }

        if (nextCarriedSecondary == nextCarriedMain) nextCarriedSecondary = null;

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

            if (nextCarriedMain != null && nextCarriedMain.IsAlive() && nextCarriedMain.movement == Vector3Int.zero)
                MoveCarriedObjectDirect(nextCarriedMain, direction, tickData);

            if (nextCarriedSecondary != null && nextCarriedSecondary.IsAlive() && nextCarriedSecondary.movement == Vector3Int.zero)
                MoveCarriedObjectDirect(nextCarriedSecondary, direction, tickData);
        }
    }

    private bool IsCellFreeOrSelf(Vector3Int pos, Object self)
    {
        Object objAtPos = gameState.GetObjectAt(pos);
        return objAtPos == null || objAtPos == self || !objAtPos.IsAlive();
    }

    private bool ProcessBoxSliding(TickData tickData)
    {
        return false;
    }

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
                if (obj.position.y <= DEATH_BOUND)
                {
                    KillObject(obj, tickData);
                    anyFalling = true;
                }
                else
                {
                    Vector3Int belowPos = obj.position + Vector3Int.down;
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

    private bool ProcessRespawning(TickData tickData)
    {
        bool anyRespawning = false;
        List<Object> allObjects = gameState.GetAllObjects();
        
        foreach (Object obj in allObjects)
        {
            Debug.Log($"[ProcessRespawning] checking {obj.type} alive:{obj.IsAlive()}");
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

    private bool IsValidMove(Vector3Int pos, Vector3Int direction)
    {
        if (direction == Vector3Int.zero) return false;

        GeoType geoType = geoState.GetGeoTypeAt(pos);
        if (geoType == GeoType.Block || geoType == GeoType.Exit) return false;
        if (geoType == GeoType.Spawn && direction != Vector3Int.down) return false;

        Object objAtPos = gameState.GetObjectAt(pos);
        if (objAtPos == null || !objAtPos.IsAlive()) return true;

        Vector3Int nextPos = pos + direction;
        if (objAtPos.type == "1x2_box")
        {
            if (pos == objAtPos.position && direction == objAtPos.rotation)
                nextPos = objAtPos.GetSecondaryPosition() + direction;
            else if (pos == objAtPos.GetSecondaryPosition() && direction == -objAtPos.rotation)
                nextPos = objAtPos.position + direction;
        }
        return IsValidMove(nextPos, direction);
    }

    private void PushObject(Object obj, Vector3Int direction, TickData tickData)
    {
        if (obj.movement != Vector3Int.zero) return;

        Vector3Int targetPos = obj.position + direction;

        Object blockingObject = gameState.GetObjectAt(targetPos);
        if (blockingObject != null && blockingObject.IsAlive() && blockingObject != obj)
            PushObject(blockingObject, direction, tickData);

        if (obj.type == "1x2_box")
        {
            Vector3Int secondaryPos = obj.GetSecondaryPosition();
            Vector3Int secondaryTarget = secondaryPos + direction;
            Object blockingSecondary = gameState.GetObjectAt(secondaryTarget);
            if (blockingSecondary != null && blockingSecondary.IsAlive() &&
                blockingSecondary != obj && blockingSecondary != blockingObject)
                PushObject(blockingSecondary, direction, tickData);
        }

        Vector3Int aboveObjOldPos1 = obj.position + Vector3Int.up;
        Object carriedByPushed1 = gameState.GetObjectAt(aboveObjOldPos1);

        Object carriedByPushed2 = null;
        if (obj.type == "1x2_box")
        {
            Vector3Int aboveObjOldPos2 = obj.GetSecondaryPosition() + Vector3Int.up;
            carriedByPushed2 = gameState.GetObjectAt(aboveObjOldPos2);
        }

        if (obj.type == "1x2_box" && carriedByPushed2 == carriedByPushed1) carriedByPushed2 = null;

        MoveObject(obj, targetPos, TaskAction.Slide, tickData);

        if (carriedByPushed1 != null && carriedByPushed1.IsAlive() && carriedByPushed1.movement == Vector3Int.zero)
            MoveCarriedObjectDirect(carriedByPushed1, direction, tickData);

        if (obj.type == "1x2_box" && carriedByPushed2 != null && carriedByPushed2.IsAlive() && carriedByPushed2.movement == Vector3Int.zero)
            MoveCarriedObjectDirect(carriedByPushed2, direction, tickData);
    }

    private void MoveObject(Object obj, Vector3Int targetPos, TaskAction actionType, TickData tickData)
    {
        Vector3Int fromPos = obj.position;
        Vector3Int moveDir = targetPos - fromPos;

        gameState.MoveObjectTo(obj, targetPos);
        obj.movement += moveDir;

        ObjectMovement movement = new ObjectMovement(obj, fromPos, targetPos, actionType);
        tickData.movements.Add(movement);

        Task task = new Task(obj.color, actionType, moveDir);
        tickData.tasks.Add(task);
    }

    private void KillObject(Object obj, TickData tickData)
    {
        Vector3Int deathPos = obj.position;
        obj.alive = false;
        gameState.RemoveObjectFromGrid(deathPos);

        Task task = new Task(obj.color, TaskAction.Die, Vector3Int.zero);
        tickData.tasks.Add(task);

        ObjectMovement movement = new ObjectMovement(obj, deathPos, deathPos, TaskAction.Die);
        tickData.movements.Add(movement);
    }

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
