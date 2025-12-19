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
        }
    }
    
    private void RegisterScaledBlock()
    {
        // Use Unity's built-in collision bounds - this is EXACTLY what Unity uses
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError($"GeoBlock on {gameObject.name}: No MeshRenderer found! Cannot calculate bounds.");
            return;
        }
        
        // Get the world-space bounds of the mesh
        Bounds bounds = renderer.bounds;
        
        // Calculate which grid cells are covered
        Vector3Int minPos = new Vector3Int(
            Mathf.FloorToInt(bounds.min.x),
            Mathf.FloorToInt(bounds.min.y),
            Mathf.FloorToInt(bounds.min.z)
        );
        Vector3Int maxPos = new Vector3Int(
            Mathf.CeilToInt(bounds.max.x) - 1,
            Mathf.CeilToInt(bounds.max.y) - 1,
            Mathf.CeilToInt(bounds.max.z) - 1
        );
        
        // Register all grid positions covered by this block
        for (int x = minPos.x; x <= maxPos.x; x++)
        {
            for (int y = minPos.y; y <= maxPos.y; y++)
            {
                for (int z = minPos.z; z <= maxPos.z; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    geoState.PlaceGeoAt(gridPos, blockType);
                    occupiedPositions.Add(gridPos);
                }
            }
        }
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
            // Use the same bounds calculation as RegisterScaledBlock
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;
            
            Bounds bounds = renderer.bounds;
            
            Vector3Int minPos = new Vector3Int(
                Mathf.FloorToInt(bounds.min.x),
                Mathf.FloorToInt(bounds.min.y),
                Mathf.FloorToInt(bounds.min.z)
            );
            Vector3Int maxPos = new Vector3Int(
                Mathf.CeilToInt(bounds.max.x) - 1,
                Mathf.CeilToInt(bounds.max.y) - 1,
                Mathf.CeilToInt(bounds.max.z) - 1
            );
            
            for (int x = minPos.x; x <= maxPos.x; x++)
            {
                for (int y = minPos.y; y <= maxPos.y; y++)
                {
                    for (int z = minPos.z; z <= maxPos.z; z++)
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