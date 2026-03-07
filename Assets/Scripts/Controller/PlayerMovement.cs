using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.1f;
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
    [Header("Respawn")]
    public GameObject prefabReference;
    
    [Header("Camera")]
    public CameraController cameraController;
    
    private GamePhysics gamePhysics;
    private GameState gameState;
    private Object playerObject;
    private float lastInputTime;
    
    private Queue<Vector3> positionQueue = new Queue<Vector3>();
    private Vector3 visualPosition;
    private bool isAnimating = false;
    private bool pendingWinCheck = false;
    
    void Start()
    {
        gamePhysics = FindObjectOfType<GamePhysics>();
        gameState = FindObjectOfType<GameState>();
        
        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();
        
        if (gamePhysics == null) { Debug.LogError("PlayerInput: GamePhysics not found!"); return; }
        if (gameState == null) { Debug.LogError("PlayerInput: GameState not found!"); return; }
        
        RegisterPlayer();
        lastInputTime = -inputCooldown;
        GamePhysics.OnStepComplete += OnStepComplete;
    }

    void OnDestroy()
    {
        GamePhysics.OnStepComplete -= OnStepComplete;
    }

    private void OnStepComplete(List<TickData> stepData)
    {
        pendingWinCheck = true;
        if (playerObject == null) return;

        foreach (TickData tick in stepData)
        {
            foreach (ObjectMovement movement in tick.movements)
            {
                if (movement.obj == playerObject)
                {
                    if (movement.movementType == TaskAction.Respawn)
                    {
                        positionQueue.Clear();
                        visualPosition = (Vector3)movement.toPosition;
                        transform.position = visualPosition;
                        gameObject.SetActive(true);
                    }
                    else if (movement.movementType == TaskAction.Die)
                    {
                        break;
                    }
                    else
                    {
                        positionQueue.Enqueue((Vector3)movement.toPosition);
                    }
                }
            }
        }
    }
    
    private void RegisterPlayer()
    {
        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);
        
        if (prefabReference == null)
            Debug.LogWarning("PlayerInput: No prefab reference assigned! Respawn won't work.");
        
        playerObject = new Object("none", "player", gridPos, Vector3Int.forward, prefabReference);
        gameState.PlaceObjectAt(playerObject, gridPos);
        visualPosition = transform.position;
    }
    
    void Update()
    {
        // Auto-trigger respawn when all animations are done and something needs respawning
        if (gamePhysics.NeedsRespawn() && positionQueue.Count == 0 && !isAnimating)
        {
            bool allBoxesDone = true;
            foreach (PushBlocks box in FindObjectsOfType<PushBlocks>(true))
            {
                if (!box.IsQueueEmpty())
                {
                    allBoxesDone = false;
                    break;
                }
            }

            if (allBoxesDone)
            {
                Debug.Log($"[AutoRespawn] All animations complete, triggering respawn");
                gamePhysics.StartStep(Vector3Int.zero);
                return;
            }
        }

        // Animate through queued positions
        if (positionQueue.Count > 0)
        {
            isAnimating = true;
            Vector3 targetPos = positionQueue.Peek();
            float step = moveSpeed * Time.deltaTime;
            visualPosition = Vector3.MoveTowards(visualPosition, targetPos, step);
            transform.position = visualPosition;
            
            if (Vector3.Distance(visualPosition, targetPos) < 0.01f)
            {
                visualPosition = targetPos;
                transform.position = targetPos;
                positionQueue.Dequeue();
            }
        }
        else
        {
            isAnimating = false;
        }
        // Check win condition after all animations finish
        if (pendingWinCheck && positionQueue.Count == 0 && !gamePhysics.NeedsRespawn())
        {
            Debug.Log($"Win check firing, queue={positionQueue.Count}");
            bool allBoxesDone = true;
            foreach (PushBlocks box in FindObjectsOfType<PushBlocks>(true))
            {
                if (!box.IsQueueEmpty())
                {
                    allBoxesDone = false;
                    break;
                }
            }

            if (allBoxesDone)
            {
                pendingWinCheck = false;
                gamePhysics.CheckWinCondition();
            }
        }

        // Handle death visibility
        if (playerObject != null && !playerObject.IsAlive() && !isAnimating)
        {
            // Keep this object active while waiting for respawn so Update keeps running.
            // Otherwise auto-respawn checks stop after SetActive(false).
            if (!gamePhysics.NeedsRespawn())
            {
                gameObject.SetActive(false);
            }
            return;
        }

        if (playerObject != null && playerObject.IsAlive() && !gameObject.activeSelf)
            gameObject.SetActive(true);

        // Input - always checked, even during animation
        if (Time.time - lastInputTime < inputCooldown) return;

        int dirIndex = -1;
        if (Input.GetKeyDown(KeyCode.W)) dirIndex = 0;
        else if (Input.GetKeyDown(KeyCode.D)) dirIndex = 1;
        else if (Input.GetKeyDown(KeyCode.S)) dirIndex = 2;
        else if (Input.GetKeyDown(KeyCode.A)) dirIndex = 3;

        if (dirIndex != -1)
        {
            Vector3Int worldDirection = GetCameraRelativeDirection(dirIndex);
            
            while (positionQueue.Count > 0)
                visualPosition = positionQueue.Dequeue();
            transform.position = visualPosition;

            foreach (PushBlocks box in FindObjectsOfType<PushBlocks>())
                box.SnapToPosition();

            if (playerObject.IsAlive())
            {
                ProcessMovement(worldDirection);
                lastInputTime = Time.time;
            }
            else if (gamePhysics.NeedsRespawn())
            {
                gamePhysics.StartStep(Vector3Int.zero);
            }
        }
    }
    
    private void ProcessMovement(Vector3Int direction)
    {
        gamePhysics.StartStep(direction);
    }
    
    private Vector3Int GetCameraRelativeDirection(int dirIndex)
    {
        Vector3Int[] dirs = {Vector3Int.back, Vector3Int.left, Vector3Int.forward, Vector3Int.right};

        if (cameraController == null) return dirs[dirIndex];
        
        float cameraYaw = cameraController.GetYawRotation();
        int indexYaw = Mathf.RoundToInt(cameraYaw / 90f);
        int index = (dirIndex + indexYaw + 4) % 4;
        
        return dirs[index];
    }
    
    public void UpdateVisualPosition()
    {
        if (playerObject != null)
        {
            visualPosition = playerObject.position;
            transform.position = visualPosition;
            positionQueue.Clear();
        }
    }
}