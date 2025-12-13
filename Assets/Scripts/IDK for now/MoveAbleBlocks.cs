using UnityEngine;

/// <summary>
/// Attach this to any GameObject to register it as a movable box in GameState
/// The box will automatically register based on its world position
/// </summary>
public class BoxObject : MonoBehaviour
{
    [Header("Box Settings")]
    [Tooltip("Color of the box")]
    public string boxColor = "green";
    
    [Tooltip("Type of box")]
    public string boxType = "box";
    
    [Header("Debug")]
    public bool showGizmo = true;
    
    private GameState gameState;
    private Object boxObject;
    
    void Start()
    {
        // Find GameState in scene
        gameState = FindObjectOfType<GameState>();
        
        if (gameState == null)
        {
            Debug.LogError($"BoxObject on {gameObject.name}: GameState not found in scene!");
            return;
        }
        
        // Calculate grid position from world position
        Vector3Int gridPosition = Vector3Int.RoundToInt(transform.position);
        
        // Create the box Object
        boxObject = new Object(boxColor, boxType, gridPosition, Direction.Forward);
        
        // Register with GameState
        gameState.PlaceObjectAt(boxObject, gridPosition);
        
        Debug.Log($"Box registered: {boxColor} {boxType} at {gridPosition}");
    }
    
    void LateUpdate()
    {
        // Keep visual position synced with game state
        if (boxObject != null && boxObject.IsAlive())
        {
            transform.position = boxObject.position;
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
