using System.Collections.Generic;
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
    
    [Tooltip("Auto-register multiple positions based on scale")]
    public bool useScale = true;
    
    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.white;
    
    private GeoState geoState;
    private List<Vector3Int> occupiedPositions = new List<Vector3Int>();
    
    void Awake()
    {
        // Find GeoState in scene
        geoState = FindObjectOfType<GeoState>();
        
        if (geoState == null)
        {
            Debug.LogError($"GeoBlock on {gameObject.name}: GeoState not found in scene!");
            return;
        }
        
        if (useScale)
        {
            // Register multiple positions based on scale
            RegisterScaledBlock();
        }
        else
        {
            // Register single position
            Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);
            geoState.PlaceGeoAt(gridPos, blockType);
            occupiedPositions.Add(gridPos);
            Debug.Log($"GeoBlock registered: {blockType} at {gridPos}");
        }
    }
    
    private void RegisterScaledBlock()
    {
        // Calculate the bounds based on position and scale
        Vector3 scale = transform.localScale;
        Vector3 center = transform.position;
        
        Debug.Log($"[GeoBlock] Registering scaled block at {center} with scale {scale}");
        
        // Calculate min and max grid positions
        // Use floor/ceil to properly cover the area
        Vector3 halfScale = scale / 2f;
        Vector3Int minPos = new Vector3Int(
            Mathf.FloorToInt(center.x - halfScale.x),
            Mathf.FloorToInt(center.y - halfScale.y),
            Mathf.FloorToInt(center.z - halfScale.z)
        );
        Vector3Int maxPos = new Vector3Int(
            Mathf.CeilToInt(center.x + halfScale.x),
            Mathf.CeilToInt(center.y + halfScale.y),
            Mathf.CeilToInt(center.z + halfScale.z)
        );
        
        Debug.Log($"[GeoBlock] Will register from {minPos} to {maxPos}");
        
        // Register all grid positions covered by this block
        // Use <= for max to include the boundary
        for (int x = minPos.x; x < maxPos.x; x++)
        {
            for (int y = minPos.y; y < maxPos.y; y++)
            {
                for (int z = minPos.z; z < maxPos.z; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    geoState.PlaceGeoAt(gridPos, blockType);
                    occupiedPositions.Add(gridPos);
                }
            }
        }
        
        Debug.Log($"[GeoBlock] Registered {occupiedPositions.Count} total positions");
    }
    
    void OnDestroy()
    {
        // Unregister all occupied positions when destroyed
        if (geoState != null)
        {
            foreach (Vector3Int pos in occupiedPositions)
            {
                geoState.RemoveGeoAt(pos);
            }
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
        
        if (useScale)
        {
            // Show all grid positions covered by this block
            Vector3 scale = transform.localScale;
            Vector3 center = transform.position;
            Vector3Int minPos = Vector3Int.RoundToInt(center - scale / 2f);
            Vector3Int maxPos = Vector3Int.RoundToInt(center + scale / 2f);
            
            for (int x = minPos.x; x < maxPos.x; x++)
            {
                for (int y = minPos.y; y < maxPos.y; y++)
                {
                    for (int z = minPos.z; z < maxPos.z; z++)
                    {
                        Vector3Int gridPos = new Vector3Int(x, y, z);
                        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.95f);
                    }
                }
            }
        }
        else
        {
            // Show single grid position
            Vector3Int pos = Vector3Int.RoundToInt(transform.position);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.95f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Highlight selected block
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, Vector3.one);
    }
}