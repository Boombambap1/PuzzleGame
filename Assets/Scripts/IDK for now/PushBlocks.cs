using UnityEngine;

public class PushBlocks : MonoBehaviour
{
    [Header("Box Settings")]
    [Tooltip("Color of the box")]
    public string boxColor = "green";
    
    [Tooltip("Type of box")]
    public string boxType = "box";

    [Tooltip("Box rotation (direction) on the grid")]
    public Vector3Int boxRotation = Vector3Int.forward;
    
    [Tooltip("Prefab reference for respawning (drag the prefab here)")]
    public GameObject prefabReference;
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
    [Header("Debug")]
    public bool showGizmo = true;
    
    private GameState gameState;
    public Object boxObject;
    
    private Vector3 visualPosition;
    private bool isAnimating = false;
    private GameObject secondaryVisual;
    
    void Start()
    {
        gameState = FindObjectOfType<GameState>();
        if (gameState == null)
        {
            Debug.LogError($"PushBlocks on {gameObject.name}: GameState not found in scene!");
            return;
        }
        
        Vector3Int gridPosition = Vector3Int.RoundToInt(transform.position);
        
        // Use the manually assigned prefab reference
        if (prefabReference == null)
        {
            Debug.LogWarning($"PushBlocks on {gameObject.name}: No prefab reference assigned! Respawn won't work.");
        }
        
        boxObject = new Object(boxColor, boxType, gridPosition, boxRotation, prefabReference);
        gameState.PlaceObjectAt(boxObject, gridPosition);
        visualPosition = transform.position;

        if (boxType == "1x2_box")
        {
            CreateSecondaryVisual();
            UpdateSecondaryVisual();
        }
        
        Debug.Log($"PushBlock registered: {boxColor} {boxType} at {gridPosition}, prefab: {(prefabReference != null ? prefabReference.name : "NONE")}");
    }
    
    void Update()
    {
        if (boxObject != null && boxObject.IsAlive())
        {
            // Re-activate if was dead and now alive (respawn)
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                visualPosition = boxObject.position;
            }
            
            Vector3 targetPos = boxObject.position;
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

            UpdateSecondaryVisual();
        }
        else if (boxObject != null && !boxObject.IsAlive())
        {
            gameObject.SetActive(false);
            if (secondaryVisual != null)
            {
                secondaryVisual.SetActive(false);
            }
        }
    }
    
    void OnDestroy()
    {
        if (gameState != null && boxObject != null)
        {
            gameState.RemoveObjectAt(boxObject.position);
        }

        if (secondaryVisual != null)
        {
            Destroy(secondaryVisual);
        }
    }

    private void CreateSecondaryVisual()
    {
        if (secondaryVisual != null) return;

        secondaryVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        secondaryVisual.name = $"{gameObject.name}_Secondary";

        BoxCollider collider = secondaryVisual.GetComponent<BoxCollider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        MeshRenderer secondaryRenderer = secondaryVisual.GetComponent<MeshRenderer>();
        if (renderer != null && secondaryRenderer != null)
        {
            secondaryRenderer.sharedMaterial = renderer.sharedMaterial;
        }
    }

    private void UpdateSecondaryVisual()
    {
        if (secondaryVisual == null || boxObject == null || !boxObject.IsAlive()) return;

        if (!secondaryVisual.activeSelf)
        {
            secondaryVisual.SetActive(true);
        }

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
