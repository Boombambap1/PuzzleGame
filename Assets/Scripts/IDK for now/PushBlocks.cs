using System.Collections.Generic;
using UnityEngine;

public class PushBlocks : MonoBehaviour
{
    [Header("Box Settings")]
    public string boxColor = "green";
    public string boxType = "box";
    public Vector3Int boxRotation = Vector3Int.forward;
    
    [Tooltip("Prefab reference for respawning (drag the prefab here)")]
    public GameObject prefabReference;
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
    [Header("Debug")]
    public bool showGizmo = true;
    
    private GameState gameState;
    private GamePhysics gamePhysics;
    public Object boxObject;
    
    private Vector3 visualPosition;
    private bool justDied = false;
    public bool IsQueueEmpty()
    {
        if (boxObject != null && !boxObject.IsAlive())
    {
        positionQueue.Clear();
        return true;
    }
    return positionQueue.Count == 0;
    }
    private Queue<Vector3> positionQueue = new Queue<Vector3>();

    void Start()
    {
        gameState = FindObjectOfType<GameState>();
        gamePhysics = FindObjectOfType<GamePhysics>();

        if (gameState == null)
        {
            Debug.LogError($"PushBlocks on {gameObject.name}: GameState not found!");
            return;
        }
        
        Vector3Int gridPosition = Vector3Int.RoundToInt(transform.position);
        
        if (prefabReference == null)
            Debug.LogWarning($"PushBlocks on {gameObject.name}: No prefab reference assigned! Respawn won't work.");
        
        boxObject = new Object(boxColor, boxType, gridPosition, boxRotation, prefabReference);
        gameState.PlaceObjectAt(boxObject, gridPosition);
        visualPosition = transform.position;

        GamePhysics.OnStepComplete += OnStepComplete;
    }

    void OnDestroy()
    {
        GamePhysics.OnStepComplete -= OnStepComplete;

        if (gameState != null && boxObject != null)
            gameState.RemoveObjectFromGrid(boxObject.position);
    }

    private void OnStepComplete(List<TickData> stepData)
    {
        if (boxObject == null) return;

        foreach (TickData tick in stepData)
        {
            foreach (ObjectMovement movement in tick.movements)
            {
                if (movement.obj == boxObject)
                {
                    Debug.Log($"Box received: {movement.movementType} to {movement.toPosition}");
                    if (movement.movementType == TaskAction.Respawn)
                    {
                        positionQueue.Clear();
                        visualPosition = (Vector3)movement.toPosition;
                        transform.position = visualPosition;
                        gameObject.SetActive(true);
                    }
                    else if (movement.movementType == TaskAction.Die)
                    {
                        justDied = true;
                        goto doneProcessing;
                    }
                    else
                    {
                        positionQueue.Enqueue((Vector3)movement.toPosition);
                    }
                }
            }
        }
        doneProcessing:;
    }
    
    void Update()
    {
        if (boxObject == null) return;

        // Animate through queued positions first, always
        if (positionQueue.Count > 0)
        {
            gameObject.SetActive(true);
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
            return; // nothing else runs while animating
        }

        // Queue is empty, NOW check alive state
        if (!boxObject.IsAlive())
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void SnapToPosition()
    {
        positionQueue.Clear();
        if (boxObject != null)
        {
            visualPosition = (Vector3)boxObject.position;
            transform.position = visualPosition;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        
        Color gizmoColor = boxColor switch
        {
            "green" => Color.green,
            "red" => Color.red,
            "blue" => Color.blue,
            "yellow" => Color.yellow,
            _ => Color.white
        };
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
    }
}
