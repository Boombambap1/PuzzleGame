using System.Collections.Generic;
using UnityEngine;

public class GeoState : MonoBehaviour
{
    // Grid storage for immovable terrain
    private Dictionary<Vector3Int, GeoType> geoGrid;
    
    // Spawn point tracking by color
    private Dictionary<string, Vector3Int> spawnPoints;
    
    void Awake()
    {
        InitializeDictionaries();
    }
    
    private void InitializeDictionaries()
    {
        if (geoGrid == null)
        {
            geoGrid = new Dictionary<Vector3Int, GeoType>();
        }
        if (spawnPoints == null)
        {
            spawnPoints = new Dictionary<string, Vector3Int>();
        }
    }
    
    /// <summary>
    /// Place a geo block at a position
    /// </summary>
    public void PlaceGeoAt(Vector3Int pos, GeoType type)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        geoGrid[pos] = type;
        
        // Track spawn points
        if (type == GeoType.Spawn)
        {
            // You can extend this to track color-specific spawns
            // For now, using position as the key
        }
    }
    
    /// <summary>
    /// Get the geo type at a position
    /// </summary>
    public GeoType GetGeoTypeAt(Vector3Int pos)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        
        if (geoGrid.TryGetValue(pos, out GeoType type))
        {
            return type;
        }
        return GeoType.Void; // Default to void if nothing placed
    }
    
    /// <summary>
    /// Remove geo block at a position
    /// </summary>
    public void RemoveGeoAt(Vector3Int pos)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        
        if (geoGrid.ContainsKey(pos))
        {
            geoGrid.Remove(pos);
        }
    }
    
    /// <summary>
    /// Clear all geo blocks from the map
    /// </summary>
    public void ClearAllGeo()
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        geoGrid.Clear();
        spawnPoints.Clear();
    }
    
    /// <summary>
    /// Check if a position is void (empty space, not a block)
    /// </summary>
    public bool CheckVoidAt(Vector3Int pos)
    {
        return GetGeoTypeAt(pos) == GeoType.Void;
    }
    
    /// <summary>
    /// Check if a specific geo type exists at a position
    /// </summary>
    public bool CheckGeoTypeAt(Vector3Int pos, GeoType type)
    {
        return GetGeoTypeAt(pos) == type;
    }
    
    /// <summary>
    /// Find spawn position for a specific color
    /// Returns null if no spawn point found
    /// </summary>
    public Vector3Int? FindSpawnPosition(string color)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        
        if (spawnPoints.TryGetValue(color, out Vector3Int spawnPos))
        {
            return spawnPos;
        }
        
        // If no color-specific spawn, find any spawn point
        foreach (var kvp in geoGrid)
        {
            if (kvp.Value == GeoType.Spawn)
            {
                return kvp.Key;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Register a spawn point for a specific color
    /// </summary>
    public void RegisterSpawnPoint(string color, Vector3Int position)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        spawnPoints[color] = position;
        PlaceGeoAt(position, GeoType.Spawn);
    }
    
    /// <summary>
    /// Load a level from a simple grid definition
    /// </summary>
    public void LoadLevel(GeoType[,,] levelData)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        ClearAllGeo();
        
        int sizeX = levelData.GetLength(0);
        int sizeY = levelData.GetLength(1);
        int sizeZ = levelData.GetLength(2);
        
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    GeoType type = levelData[x, y, z];
                    if (type != GeoType.Void)
                    {
                        PlaceGeoAt(new Vector3Int(x, y, z), type);
                    }
                }
            }
        }
    }
}


