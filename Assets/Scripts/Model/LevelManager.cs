using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public GeoState geoState;
    public GameState gameState;
    public GamePhysics gamePhysics;
    
    [Header("Debug Visualization")]
    public bool showDebugGrid = true;
    public Material blockMaterial;
    public Material floorMaterial;
    public Material playerMaterial;
    public Material boxMaterial;
    
    private Dictionary<Vector3Int, GameObject> visualObjects;
    
    void Awake()
    {
        visualObjects = new Dictionary<Vector3Int, GameObject>();
        
        // Get or create references
        if (geoState == null) geoState = GetComponent<GeoState>();
        if (gameState == null) gameState = GetComponent<GameState>();
        if (gamePhysics == null) gamePhysics = GetComponent<GamePhysics>();
    }
    
    void Start()
    {
        // Load a simple test level
        LoadTestLevel();
    }
    
    /// <summary>
    /// Create a simple test level - a 5x5 platform with walls
    /// </summary>
    public void LoadTestLevel()
    {
        Debug.Log("Loading test level...");
        
        // Clear existing
        geoState.ClearAllGeo();
        gameState.ClearAllObjects();
        ClearVisuals();
        
        // Create floor (5x5 at y=0)
        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                geoState.PlaceGeoAt(pos, GeoType.Block);
                CreateVisualBlock(pos, GeoType.Block);
            }
        }
        
        // Create walls around the perimeter at y=1
        for (int x = 0; x < 5; x++)
        {
            Vector3Int northWall = new Vector3Int(x, 1, 4);
            Vector3Int southWall = new Vector3Int(x, 1, 0);
            geoState.PlaceGeoAt(northWall, GeoType.Block);
            geoState.PlaceGeoAt(southWall, GeoType.Block);
            CreateVisualBlock(northWall, GeoType.Block);
            CreateVisualBlock(southWall, GeoType.Block);
        }
        
        for (int z = 1; z < 4; z++)
        {
            Vector3Int eastWall = new Vector3Int(4, 1, z);
            Vector3Int westWall = new Vector3Int(0, 1, z);
            geoState.PlaceGeoAt(eastWall, GeoType.Block);
            geoState.PlaceGeoAt(westWall, GeoType.Block);
            CreateVisualBlock(eastWall, GeoType.Block);
            CreateVisualBlock(westWall, GeoType.Block);
        }
        
        // Place spawn point
        Vector3Int spawnPos = new Vector3Int(2, 1, 2);
        geoState.RegisterSpawnPoint("player", spawnPos);
        
        // Create player
        Object player = new Object("none", "robot", spawnPos, Direction.Forward);
        gameState.PlaceObjectAt(player, spawnPos);
        CreateVisualObject(player);
        
        // Create a test box
        Vector3Int boxPos = new Vector3Int(3, 1, 2);
        Object box = new Object("green", "box", boxPos, Direction.Forward);
        gameState.PlaceObjectAt(box, boxPos);
        CreateVisualObject(box);
        
        Debug.Log("Test level loaded!");
        Debug.Log($"Player at: {player.position}");
        Debug.Log($"Box at: {box.position}");
    }
    
    /// <summary>
    /// Create visual representation of a geo block
    /// </summary>
    private void CreateVisualBlock(Vector3Int pos, GeoType type)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = pos;
        cube.name = $"Geo_{type}_{pos}";
        
        // Apply material based on type
        if (type == GeoType.Block)
        {
            if (pos.y == 0 && floorMaterial != null)
            {
                cube.GetComponent<Renderer>().material = floorMaterial;
            }
            else if (blockMaterial != null)
            {
                cube.GetComponent<Renderer>().material = blockMaterial;
            }
            else
            {
                // Default colors
                cube.GetComponent<Renderer>().material.color = pos.y == 0 ? Color.gray : Color.white;
            }
        }
        
        visualObjects[pos] = cube;
    }
    
    /// <summary>
    /// Create visual representation of a game object
    /// </summary>
    private void CreateVisualObject(Object obj)
    {
        GameObject visual;
        
        if (obj.type == "robot")
        {
            // Player is a capsule
            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Player";
            
            if (playerMaterial != null)
            {
                visual.GetComponent<Renderer>().material = playerMaterial;
            }
            else
            {
                visual.GetComponent<Renderer>().material.color = Color.blue;
            }
        }
        else
        {
            // Boxes are cubes
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = $"Box_{obj.color}_{obj.position}";
            
            if (boxMaterial != null)
            {
                visual.GetComponent<Renderer>().material = boxMaterial;
            }
            
            // Color based on object color
            Color visualColor = obj.color switch
            {
                "green" => Color.green,
                "red" => Color.red,
                "blue" => Color.cyan,
                "yellow" => Color.yellow,
                _ => Color.white
            };
            visual.GetComponent<Renderer>().material.color = visualColor;
        }
        
        visual.transform.position = obj.position;
        visual.transform.localScale = obj.type == "robot" ? new Vector3(0.5f, 0.5f, 0.5f) : Vector3.one * 0.8f;
        
        // Store reference
        visualObjects[obj.position] = visual;
    }
    
    /// <summary>
    /// Clear all visual objects
    /// </summary>
    private void ClearVisuals()
    {
        foreach (var visual in visualObjects.Values)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }
        visualObjects.Clear();
    }
    
    /// <summary>
    /// Update visuals to match current game state
    /// </summary>
    public void UpdateVisuals()
    {
        // Clear old object visuals
        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var kvp in visualObjects)
        {
            if (kvp.Value.name.StartsWith("Box_") || kvp.Value.name == "Player")
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var pos in toRemove)
        {
            visualObjects.Remove(pos);
        }
        
        // Create new object visuals
        foreach (Object obj in gameState.GetAllObjects())
        {
            if (obj.IsAlive())
            {
                CreateVisualObject(obj);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGrid) return;
        
        // Draw grid
        Gizmos.color = Color.yellow;
        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {
                Vector3 center = new Vector3(x, 0.5f, z);
                Gizmos.DrawWireCube(center, Vector3.one * 0.9f);
            }
        }
    }
}
