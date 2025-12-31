using System.Collections.Generic;
using UnityEngine;

public class GeoState : MonoBehaviour
{
    // Grid storage for immovable terrain
    private Dictionary<Vector3Int, GeoType> geoGrid;
    
    // Spawn point tracking by prefab
    private Dictionary<GameObject, Vector3Int> spawnPoints;
    
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
            spawnPoints = new Dictionary<GameObject, Vector3Int>();
        }
    }
    
    /// <summary>
    /// Place a geo block at a position
    /// </summary>
    public void PlaceGeoAt(Vector3Int pos, GeoType type)
    {
        InitializeDictionaries();
        geoGrid[pos] = type;
    }
    
    /// <summary>
    /// Get the geo type at a position
    /// </summary>
    public GeoType GetGeoTypeAt(Vector3Int pos)
    {
        InitializeDictionaries();
        
        if (geoGrid.TryGetValue(pos, out GeoType type))
        {
            return type;
        }
        return GeoType.Void;
    }
    
    /// <summary>
    /// Remove geo block at a position
    /// </summary>
    public void RemoveGeoAt(Vector3Int pos)
    {
        InitializeDictionaries();
        
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
        InitializeDictionaries();
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
    /// Find spawn position for a specific prefab
    /// Returns null if no spawn point found
    /// </summary>
    public Vector3Int? FindSpawnPosition(GameObject prefab)
    {
        InitializeDictionaries();
        
        if (spawnPoints.TryGetValue(prefab, out Vector3Int spawnPos))
        {
            return spawnPos;
        }
        
        Debug.LogError($"[GeoState] No spawn point found for prefab '{prefab.name}'!");
        return null;
    }
    
    /// <summary>
    /// Register a spawn point for a specific prefab
    /// Called by GeoBlocks with Spawn type
    /// </summary>
    public void RegisterSpawnPoint(GameObject prefab, Vector3Int position)
    {
        InitializeDictionaries();
        spawnPoints[prefab] = position;
        Debug.Log($"[GeoState] Registered spawn point for prefab '{prefab.name}' at {position}");
    }
    
    /// <summary>
    /// Load a level from a simple grid definition
    /// </summary>
    public void LoadLevel(GeoType[,,] levelData)
    {
        InitializeDictionaries();
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


