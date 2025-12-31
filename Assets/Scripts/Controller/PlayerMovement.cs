using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    public float inputCooldown = 0.1f;
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
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
        playerObject = new Object("none", "robot", gridPos, Direction.Forward, gameObject);
        gameState.PlaceObjectAt(playerObject, gridPos);
        visualPosition = transform.position;
    }
    
    void Update()
    {
        if (playerObject != null)
        {
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
        
        if (inputDirection != Direction.None)
        {
            ProcessMovement(inputDirection);
            lastInputTime = Time.time;
        }
    }
    
    private void ProcessMovement(Direction direction)
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