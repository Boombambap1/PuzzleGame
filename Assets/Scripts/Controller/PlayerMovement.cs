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
        if (gamePhysics.NeedsRespawn() && positionQueue.Count == 0)
        {
            Debug.Log($"[AutoRespawn] triggered, allBoxesDone check starting");
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
                Debug.Log($"[AutoRespawn] triggered, allBoxesDone check starting");
                gamePhysics.StartStep(Vector3Int.zero);
                return;
            }
            else
            {
                Debug.Log($"[AutoRespawn] boxes not done yet");
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
            gameObject.SetActive(false);
            return;
        }

        if (playerObject != null && playerObject.IsAlive() && !gameObject.activeSelf)
            gameObject.SetActive(true);

        // Input - always checked, even during animation
        if (Time.time - lastInputTime < inputCooldown) return;

        Vector3Int inputDirection = Vector3Int.zero;
        if (Input.GetKeyDown(KeyCode.W)) inputDirection = Vector3Int.forward;
        else if (Input.GetKeyDown(KeyCode.S)) inputDirection = Vector3Int.back;
        else if (Input.GetKeyDown(KeyCode.A)) inputDirection = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDirection = Vector3Int.right;

        if (inputDirection != Vector3Int.zero)
        {
            while (positionQueue.Count > 0)
                visualPosition = positionQueue.Dequeue();
            transform.position = visualPosition;

            foreach (PushBlocks box in FindObjectsOfType<PushBlocks>())
                box.SnapToPosition();

            if (playerObject.IsAlive())
            {
                ProcessMovement(inputDirection);
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