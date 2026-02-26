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
    private bool isAnimating = false;
    private GameObject secondaryVisual;
    
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

        if (boxType == "1x2_box")
        {
            CreateSecondaryVisual();
            UpdateSecondaryVisual();
        }

        // Subscribe to step events so we can queue positions after each step
        GamePhysics.OnStepComplete += OnStepComplete;
        
        Debug.Log($"PushBlock registered: {boxColor} {boxType} at {gridPosition}, prefab: {(prefabReference != null ? prefabReference.name : "NONE")}");
    }

    void OnDestroy()
    {
        GamePhysics.OnStepComplete -= OnStepComplete;

        if (gameState != null && boxObject != null)
            gameState.RemoveObjectFromGrid(boxObject.position);

        if (secondaryVisual != null)
            Destroy(secondaryVisual);
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
                    positionQueue.Enqueue((Vector3)movement.toPosition);
                }
            }
        }
    }
    
    void Update()
    {
        if (boxObject == null) return;

        // Handle respawn: re-enable and snap to new position
        if (boxObject.IsAlive() && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            positionQueue.Clear();
            visualPosition = boxObject.position;
            transform.position = visualPosition;
        }

        // Hide if dead and done animating
        if (!boxObject.IsAlive() && positionQueue.Count == 0)
        {
            gameObject.SetActive(false);
            if (secondaryVisual != null) secondaryVisual.SetActive(false);
            return;
        }

        // Animate through queued positions
        if (positionQueue.Count > 0)
        {
            Vector3 targetPos = positionQueue.Peek();
            float step = moveSpeed * Time.deltaTime;
            visualPosition = Vector3.MoveTowards(visualPosition, targetPos, step);
            transform.position = visualPosition;
            isAnimating = true;

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

        UpdateSecondaryVisual();
    }
    
    private void CreateSecondaryVisual()
    {
        if (secondaryVisual != null) return;

        secondaryVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        secondaryVisual.name = $"{gameObject.name}_Secondary";

        BoxCollider collider = secondaryVisual.GetComponent<BoxCollider>();
        if (collider != null) Destroy(collider);

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        MeshRenderer secondaryRenderer = secondaryVisual.GetComponent<MeshRenderer>();
        if (renderer != null && secondaryRenderer != null)
            secondaryRenderer.sharedMaterial = renderer.sharedMaterial;
    }

    private void UpdateSecondaryVisual()
    {
        if (secondaryVisual == null || boxObject == null || !boxObject.IsAlive()) return;

        if (!secondaryVisual.activeSelf) secondaryVisual.SetActive(true);
        secondaryVisual.transform.position = boxObject.GetSecondaryPosition();
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
