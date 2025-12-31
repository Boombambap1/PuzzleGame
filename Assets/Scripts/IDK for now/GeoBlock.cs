using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to any GameObject to register it as a geo block in GeoState
/// Each block occupies exactly one 1x1x1 grid cell at its rounded position
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
    private Vector3Int occupiedPosition;
    
    void Awake()
    {
        // Find GeoState in scene
        geoState = FindObjectOfType<GeoState>();
        
        if (geoState == null)
        {
            Debug.LogError($"GeoBlock on {gameObject.name}: GeoState not found in scene!");
            return;
        }
        
        // Register single 1x1x1 block at rounded position
        RegisterSingleBlock();
    }
    
    private void RegisterSingleBlock()
    {
        // Round the transform position to nearest integer grid position
        occupiedPosition = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        // Register this single position
        geoState.PlaceGeoAt(occupiedPosition, blockType);
        
        Debug.Log($"GeoBlock '{gameObject.name}': Registered {blockType} at {occupiedPosition}");
    }
    
    void OnDestroy()
    {
        // Unregister position when destroyed
        if (geoState != null)
        {
            geoState.RemoveGeoAt(occupiedPosition);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
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
        
        // Show the single grid cell this block occupies
        Vector3Int gridPos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.95f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Highlight selected block in yellow
        Vector3Int pos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, Vector3.one);
    }
}