using UnityEngine;

/// <summary>
/// Attach this to any GameObject to register it as a pushable box in GameState
/// The box will automatically register based on its world position and animate smoothly
/// </summary>
public class PushBlocks : MonoBehaviour
{
    [Header("Box Settings")]
    [Tooltip("Color of the box")]
    public string boxColor = "green";
    
    [Tooltip("Type of box")]
    public string boxType = "box";
    
    [Header("Animation")]
    public float moveSpeed = 5f;
    
    [Header("Debug")]
    public bool showGizmo = true;
    
    private GameState gameState;
    private Object boxObject;
    
    // For smooth movement
    private Vector3 visualPosition;
    private bool isAnimating = false;
    
    void Start()
    {
        // Find GameState in scene
        gameState = FindObjectOfType<GameState>();
        
        if (gameState == null)
        {
            Debug.LogError($"PushBlocks on {gameObject.name}: GameState not found in scene!");
            return;
        }
        
        // Calculate grid position from world position
        Vector3Int gridPosition = Vector3Int.RoundToInt(transform.position);
        
        // Create the box Object
        boxObject = new Object(boxColor, boxType, gridPosition, Direction.Forward);
        
        // Register with GameState
        gameState.PlaceObjectAt(boxObject, gridPosition);
        
        // Initialize visual position
        visualPosition = transform.position;
        
        Debug.Log($"PushBlock registered: {boxColor} {boxType} at {gridPosition}");
    }
    
    void Update()
    {
        // Smooth animation
        if (boxObject != null && boxObject.IsAlive())
        {
            Vector3 targetPos = boxObject.position;
            float distance = Vector3.Distance(visualPosition, targetPos);
            
            if (distance > 0.01f)
            {
                // Animate towards target
                float step = moveSpeed * Time.deltaTime;
                visualPosition = Vector3.MoveTowards(visualPosition, targetPos, step);
                transform.position = visualPosition;
                isAnimating = true;
            }
            else
            {
                // Snap to final position
                visualPosition = targetPos;
                transform.position = targetPos;
                isAnimating = false;
            }
        }
        else if (boxObject != null && !boxObject.IsAlive())
        {
            // Hide or destroy visual if box is dead
            gameObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed
        if (gameState != null && boxObject != null)
        {
            gameState.RemoveObjectAt(boxObject.position);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        
        // Color based on box color
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
