using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.1f;
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
    [Header("Respawn")]
    [Tooltip("Prefab reference for respawning (drag the player prefab here)")]
    public GameObject prefabReference;
    
    private GamePhysics gamePhysics;
    private GameState gameState;
    private Object playerObject;
    private float lastInputTime;
    private bool isProcessingStep = false;
    
    private Vector3 visualPosition;
    private bool isAnimating = false;
    
    void Start()
    {
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
        
        RegisterPlayer();
        lastInputTime = -inputCooldown;
    }
    
    private void RegisterPlayer()
    {
        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);
        
        if (prefabReference == null)
        {
            Debug.LogWarning("PlayerInput: No prefab reference assigned! Respawn won't work.");
        }
        
        playerObject = new Object("none", "player", gridPos, Vector3Int.forward, prefabReference);
        gameState.PlaceObjectAt(playerObject, gridPos);
        visualPosition = transform.position;
    }
    
    void Update()
    {
        if (playerObject != null)
        {
            // Re-activate if was dead and now alive (respawn)
            if (playerObject.IsAlive() && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                visualPosition = playerObject.position;
            }
            
            Vector3 targetPos = playerObject.position;
            float distance = Vector3.Distance(visualPosition, targetPos);
            
            if (distance > 0.01f)
            {
                float step = moveSpeed * Time.deltaTime;
                visualPosition = Vector3.MoveTowards(visualPosition, targetPos, step);
                transform.position = visualPosition;
                isAnimating = true;
            }
            else
            {
                visualPosition = targetPos;
                transform.position = targetPos;
                isAnimating = false;
            }
            
            // Hide player if dead
            if (!playerObject.IsAlive() && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        
        if (playerObject == null || !playerObject.IsAlive())
        {
            return;
        }
        
        if (isAnimating)
        {
            return;
        }
        
        if (isProcessingStep)
        {
            return;
        }
        
        if (Time.time - lastInputTime < inputCooldown)
        {
            return;
        }
        
        Vector3Int inputDirection = Vector3Int.zero;
        if (Input.GetKeyDown(KeyCode.W))
        {
            inputDirection = Vector3Int.forward;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            inputDirection = Vector3Int.back;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            inputDirection = Vector3Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            inputDirection = Vector3Int.right;
        }
        
        if (inputDirection != Vector3Int.zero)
        {
            ProcessMovement(inputDirection);
            lastInputTime = Time.time;
        }
    }
    
    private void ProcessMovement(Vector3Int direction)
    {
        isProcessingStep = true;
        List<TickData> stepData = gamePhysics.StartStep(direction);
        isProcessingStep = false;
    }
    
    public void UpdateVisualPosition()
    {
        if (playerObject != null)
        {
            visualPosition = playerObject.position;
            transform.position = visualPosition;
        }
    }
}