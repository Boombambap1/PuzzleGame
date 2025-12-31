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
    
    [Tooltip("For Exit blocks: which color box should reach this exit")]
    public string exitColor = "none";
    
    [Tooltip("For Spawn blocks: which box prefab to spawn here")]
    public GameObject spawnPrefab = null;
    
    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.white;
    
    private GeoState geoState;
    private GameState gameState;
    private Vector3Int occupiedPosition;
    
    void Awake()
    {
        // Find GeoState in scene
        geoState = FindObjectOfType<GeoState>();
        gameState = FindObjectOfType<GameState>();
        
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
        
        // If this is an Exit block, register it as a win condition
        if (blockType == GeoType.Exit && gameState != null)
        {
            gameState.RegisterWinCondition(exitColor, occupiedPosition);
            Debug.Log($"GeoBlock '{gameObject.name}': Registered {exitColor} Exit at {occupiedPosition}");
        }
        // If this is a Spawn block, register it as a spawn point
        else if (blockType == GeoType.Spawn && geoState != null)
        {
            if (spawnPrefab != null)
            {
                geoState.RegisterSpawnPoint(spawnPrefab, occupiedPosition);
                Debug.Log($"GeoBlock '{gameObject.name}': Registered Spawn for {spawnPrefab.name} at {occupiedPosition}");
            }
            else
            {
                Debug.LogWarning($"GeoBlock '{gameObject.name}': Spawn block has no prefab assigned!");
            }
        }
        else
        {
            Debug.Log($"GeoBlock '{gameObject.name}': Registered {blockType} at {occupiedPosition}");
        }
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
            GeoType.Spawn => GetSpawnColor(),
            GeoType.Exit => GetExitColor(),
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
        
        // Draw wire cube
        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.95f);
        
        // If it's an exit or spawn, draw a filled cube inside to make it more visible
        if (blockType == GeoType.Exit || blockType == GeoType.Spawn)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
            Gizmos.DrawCube(gridPos, Vector3.one * 0.7f);
        }
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
    
    /// <summary>
    /// Get color for exit blocks based on exitColor field
    /// </summary>
    private Color GetExitColor()
    {
        return exitColor.ToLower() switch
        {
            "red" => Color.red,
            "green" => Color.green,
            "blue" => Color.blue,
            "yellow" => Color.yellow,
            "cyan" => Color.cyan,
            "magenta" => Color.magenta,
            _ => Color.cyan // Default exit color
        };
    }
    
    /// <summary>
    /// Get color for spawn blocks based on assigned prefab
    /// </summary>
    private Color GetSpawnColor()
    {
        if (spawnPrefab != null)
        {
            // Try to get color from the prefab's PushBlocks component
            PushBlocks pushBlock = spawnPrefab.GetComponent<PushBlocks>();
            if (pushBlock != null)
            {
                return pushBlock.boxColor.ToLower() switch  // Changed from .color to .boxColor
                {
                    "red" => Color.red,
                    "green" => Color.green,
                    "blue" => Color.blue,
                    "yellow" => Color.yellow,
                    "cyan" => Color.cyan,
                    "magenta" => Color.magenta,
                    _ => Color.green
                };
            }
        }
        return Color.green; // Default spawn color
    }
}