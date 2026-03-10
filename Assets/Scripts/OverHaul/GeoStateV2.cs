using System.Collections.Generic;
using UnityEngine;

namespace NewArch
{

/// <summary>
/// Stores immovable geometry tiles and provides spatial queries.
/// Part of the Model layer (MVC).
/// </summary>
public class GeoStateV2 : MonoBehaviour
{
    private Dictionary<Vector3Int, GeoType>    geoGrid     = new();
    private Dictionary<GameObject, Vector3Int> spawnPoints = new();

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void PlaceGeoAt(Vector3Int pos, GeoType type) => geoGrid[pos] = type;
    public void RemoveGeoAt(Vector3Int pos)               => geoGrid.Remove(pos);

    public void ClearAllGeo()
    {
        geoGrid.Clear();
        spawnPoints.Clear();
    }

    /// <summary>
    /// Register a prefab identity key with the grid position of its Spawn tile.
    /// Called by GeoBlockV2 (or LevelJsonLoaderV2) for every Spawn-type block.
    /// </summary>
    public void RegisterSpawnPoint(GameObject prefab, Vector3Int pos)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[GeoStateV2] RegisterSpawnPoint called with null prefab.");
            return;
        }
        spawnPoints[prefab] = pos;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public bool    CheckVoidAt(Vector3Int pos)               => !geoGrid.ContainsKey(pos);
    public bool    CheckGeoTypeAt(Vector3Int pos, GeoType t) => GetGeoTypeAt(pos) == t;
    public bool    IsSolidAt(Vector3Int pos)                 => GetGeoTypeAt(pos) == GeoType.Block || GetGeoTypeAt(pos) == GeoType.Exit;

    public GeoType GetGeoTypeAt(Vector3Int pos)
    {
        return geoGrid.TryGetValue(pos, out GeoType type) ? type : GeoType.None;
    }

    /// <summary>
    /// Returns the Spawn tile position registered for the given prefab.
    /// Returns null if no spawn point has been registered for that prefab.
    /// </summary>
    public Vector3Int? FindSpawnPosition(GameObject prefab)
    {
        if (prefab != null && spawnPoints.TryGetValue(prefab, out Vector3Int pos))
            return pos;

        return null;
    }
}

} // namespace NewArch
