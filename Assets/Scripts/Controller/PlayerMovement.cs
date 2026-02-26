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
    
    void Start()
    {
        gamePhysics = FindObjectOfType<GamePhysics>();
        gameState = FindObjectOfType<GameState>();
        
        if (gamePhysics == null) { Debug.LogError("PlayerInput: GamePhysics not found!"); return; }
        if (gameState == null) { Debug.LogError("PlayerInput: GameState not found!"); return; }
        
        RegisterPlayer();
        lastInputTime = -inputCooldown;
    }
    
    private void RegisterPlayer()
    {
        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);
        
        if (prefabReference == null)
            Debug.LogWarning("PlayerInput: No prefab reference assigned! Respawn won't work.");
        
        playerObject = new Object("none", "player", gridPos, Vector3Int.forward, prefabReference);
        gameState.PlaceObjectAt(playerObject, gridPos);
        visualPosition = transform.position;
        Debug.Log($"Player registered at {gridPos}, with visual position {visualPosition}");
    }
    
    void Update()
    {
        // Handle death visibility
        if (playerObject != null)
        {
            if (playerObject.IsAlive() && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            if (!playerObject.IsAlive() && gameObject.activeSelf && !isAnimating)
            {
                gameObject.SetActive(false);
            }
        }
        
        // Animate through queued positions one step at a time
        if (positionQueue.Count > 0)
        {
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
            
            isAnimating = true;
            return;
        }
        else
        {
            isAnimating = false;
        }
        
        // Input
        if (playerObject == null || !playerObject.IsAlive()) return;
        if (Time.time - lastInputTime < inputCooldown) return;
        
        Vector3Int inputDirection = Vector3Int.zero;
        if (Input.GetKeyDown(KeyCode.W)) inputDirection = Vector3Int.forward;
        else if (Input.GetKeyDown(KeyCode.S)) inputDirection = Vector3Int.back;
        else if (Input.GetKeyDown(KeyCode.A)) inputDirection = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDirection = Vector3Int.right;
        
        if (inputDirection != Vector3Int.zero)
        {
            ProcessMovement(inputDirection);
            lastInputTime = Time.time;
        }
    }
    
    private void ProcessMovement(Vector3Int direction)
    {
        List<TickData> stepData = gamePhysics.StartStep(direction);
        
        if (stepData == null) return;
        
        // Queue up each tick's player position sequentially
        foreach (TickData tick in stepData)
        {
            foreach (ObjectMovement movement in tick.movements)
            {
                if (movement.obj == playerObject)
                {
                    positionQueue.Enqueue(movement.toPosition);
                }
            }
        }
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