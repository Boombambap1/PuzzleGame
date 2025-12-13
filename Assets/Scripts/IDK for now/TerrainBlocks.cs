using UnityEngine;

/// <summary>
/// Attach this to any GameObject to register it as a geo block in GeoState
/// The block will automatically register based on its world position
/// </summary>
public class GeoBlock : MonoBehaviour
{
    [Header("Block Settings")]
    [Tooltip("Type of geo block")]
    public GeoType blockType = GeoType.Block;
    
    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.white;
    
    private GeoState geoState;
    private Vector3Int gridPosition;
    
    void Awake()
    {
        // Find GeoState in scene
        geoState = FindObjectOfType<GeoState>();
        
        if (geoState == null)
        {
            Debug.LogError($"GeoBlock on {gameObject.name}: GeoState not found in scene!");
            return;
        }
        
        // Calculate grid position from world position
        gridPosition = Vector3Int.RoundToInt(transform.position);
        
        // Register this block with GeoState
        geoState.PlaceGeoAt(gridPosition, blockType);
        
        Debug.Log($"GeoBlock registered: {blockType} at {gridPosition}");
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed (useful for editor)
        if (geoState != null)
        {
            geoState.RemoveGeoAt(gridPosition);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        // Show the grid position this block occupies
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        
        // Color based on block type
        Color color = blockType switch
        {
            GeoType.Block => Color.white,
            GeoType.Spawn => Color.green,
            GeoType.Exit => Color.cyan,
            GeoType.Void => Color.red,
            _ => Color.gray
        };
        
        if (gizmoColor != Color.white)
        {
            color = gizmoColor;
        }
        
        Gizmos.color = color;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.95f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Highlight selected block
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, Vector3.one);
    }
}
