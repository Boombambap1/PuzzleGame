using System.Collections.Generic;
using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Parses a level JSON file and populates GeoStateV2, GameStateV2, and
/// AnimationControlV2 with the resulting geometry and game objects.
///
/// Kept parallel to the original LevelJsonLoader so the old version remains
/// untouched as a fallback. Swap which loader is active in the scene to
/// switch between old and new systems.
///
/// JSON format is identical to the original — no level files need to change.
/// </summary>
public class LevelJsonLoaderV2 : MonoBehaviour
{
    // ── Dependencies ──────────────────────────────────────────────────────────

    private GeoStateV2         geoState;
    private GameStateV2        gameState;
    private AnimationControlV2 animationControl;
    private CameraController   cameraController;

    // ── Prefab Identity Keys ──────────────────────────────────────────────────
    // These invisible GameObjects act as stable identity keys so physics and
    // win conditions can match objects to spawn/exit points by reference.

    private GameObject greenPrefabKey;
    private GameObject redPrefabKey;
    private GameObject bluePrefabKey;
    private GameObject yellowPrefabKey;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Geo Models")]
    [SerializeField] private GameObject baseBlockModel;
    [SerializeField] private GameObject[] spawnModels = new GameObject[4]; // green, red, blue, yellow
    [SerializeField] private GameObject[] exitModels  = new GameObject[4];

    [Header("Object Models")]
    [SerializeField] private GameObject[] boxModels   = new GameObject[3]; // red, blue, yellow
    [SerializeField] private GameObject   playerModel;

    // ── Private Constants ─────────────────────────────────────────────────────

    private static readonly string[] ColorNames = { "green", "red", "blue", "yellow" };

    // ── JSON Data Classes ─────────────────────────────────────────────────────

    [System.Serializable] private class TilePosition  { public int x, y, z; }
    [System.Serializable] private class TileEntry     { public TilePosition position; public int tile_id; }
    [System.Serializable] private class SerVec3       { public float x, y, z; public Vector3 ToVector3() => new(x, y, z); }

    [System.Serializable]
    private class CameraSettings
    {
        public SerVec3 origin;
        public SerVec3 offset;
        public float   zoom          = 5f;
        public float   yawRotation   = 0f;
        public float   pitchRotation = -45f;
    }

    [System.Serializable]
    private class LevelRoot
    {
        public TileEntry[]     tiles;
        public CameraSettings  camera;
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        geoState         = FindFirstObjectByType<GeoStateV2>();
        gameState        = FindFirstObjectByType<GameStateV2>();
        animationControl = FindFirstObjectByType<AnimationControlV2>();
        cameraController = FindFirstObjectByType<CameraController>();

        if (geoState         == null) Debug.LogError("[LevelJsonLoaderV2] GeoStateV2 not found.");
        if (gameState        == null) Debug.LogError("[LevelJsonLoaderV2] GameStateV2 not found.");
        if (animationControl == null) Debug.LogError("[LevelJsonLoaderV2] AnimationControlV2 not found.");

        CreatePrefabKeys();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Parse and load a level from a JSON string.</summary>
    public void LoadLevelFromJson(string json)
    {
        // Clear previous level state.
        geoState.ClearAllGeo();
        gameState.ClearAllObjects();
        ClearVisuals();

        // Wrap bare array JSON if needed.
        string trimmed = json.Trim();
        string wrapped = trimmed.StartsWith("[") ? "{\"tiles\":" + trimmed + "}" : trimmed;

        LevelRoot levelData = JsonUtility.FromJson<LevelRoot>(wrapped);
        if (levelData?.tiles == null)
        {
            Debug.LogError("[LevelJsonLoaderV2] Failed to parse level JSON.");
            return;
        }

        // ── Pass 1: categorise tiles ─────────────────────────────────────────

        // geo positions that need a non-Block geo type (spawn/exit override)
        var geoOverrides = new Dictionary<Vector3Int, (GeoType type, int tileId)>();

        // spawn groups: tileId → list of spawn marker positions (one cell ABOVE the geo tile)
        var spawnGroups  = new Dictionary<int, List<Vector3Int>>();

        // plain geo tiles (tile_id == 0)
        var geoEntries   = new List<TileEntry>();

        foreach (TileEntry entry in levelData.tiles)
        {
            if (entry?.position == null) continue;

            if (entry.tile_id == 0)
            {
                geoEntries.Add(entry);
            }
            else if (entry.tile_id >= 1 && entry.tile_id <= 8)
            {
                Vector3Int markerPos = GetPos(entry);         // position of the marker tile
                Vector3Int geoPos    = markerPos + Vector3Int.down; // geo tile one below
                GameObject prefabKey = GetPrefabKeyByIndex((entry.tile_id - 1) % 4);

                if (entry.tile_id <= 4)
                {
                    // Spawn marker
                    geoOverrides[geoPos] = (GeoType.Spawn, entry.tile_id);

                    if (!spawnGroups.ContainsKey(entry.tile_id))
                        spawnGroups[entry.tile_id] = new List<Vector3Int>();
                    spawnGroups[entry.tile_id].Add(markerPos);
                }
                else
                {
                    // Exit marker
                    geoOverrides[geoPos] = (GeoType.Exit, entry.tile_id);
                    if (prefabKey != null)
                        gameState.RegisterWinCondition(prefabKey, geoPos);
                }
            }
            else
            {
                Debug.LogWarning($"[LevelJsonLoaderV2] Unknown tile_id {entry.tile_id} — skipped.");
            }
        }

        // ── Pass 2: place geo blocks ──────────────────────────────────────────

        foreach (TileEntry entry in geoEntries)
        {
            Vector3Int pos = GetPos(entry);

            if (geoOverrides.TryGetValue(pos, out var info))
            {
                geoState.PlaceGeoAt(pos, info.type);
                SpawnGeoVisual(pos, info.type, info.tileId);
            }
            else
            {
                geoState.PlaceGeoAt(pos, GeoType.Block);
                SpawnGeoVisual(pos, GeoType.Block, 0);
            }
        }

        // ── Pass 3: spawn game objects ────────────────────────────────────────

        foreach (KeyValuePair<int, List<Vector3Int>> group in spawnGroups)
            SpawnObjectGroup(group.Key, group.Value);

        // ── Pass 4: camera ────────────────────────────────────────────────────

        ApplyCamera(levelData);
    }

    // ── Object Spawning ───────────────────────────────────────────────────────

    private void SpawnObjectGroup(int tileId, List<Vector3Int> positions)
    {
        int         colorIndex = (tileId - 1) % 4;
        string      color      = ColorNames[colorIndex];
        var         used       = new HashSet<int>();

        for (int i = 0; i < positions.Count; i++)
        {
            if (used.Contains(i)) continue;

            int pairedIndex = FindAdjacentPair(positions, i, used);

            if (pairedIndex >= 0)
            {
                // Two adjacent spawn markers → 1x2 box
                Vector3Int rotation = positions[pairedIndex] - positions[i];
                used.Add(i);
                used.Add(pairedIndex);
                SpawnLargeBox(positions[i], color, -rotation);
            }
            else
            {
                used.Add(i);
                if (tileId == 1)
                    SpawnPlayer(positions[i]);
                else
                    SpawnBox(positions[i], color);
            }
        }
    }

    /// <summary>Spawn the player, register with GameStateV2 and AnimationControlV2.</summary>
    private void SpawnPlayer(Vector3Int pos)
    {
        GameObject prefabKey = greenPrefabKey;

        // Spawn visual
        GameObject model = playerModel != null
            ? Instantiate(playerModel, (Vector3)pos, Quaternion.identity, levelRoot)
            : CreateFallbackCube(pos, "Player");

        model.name = "Player";

        // Create logical object
        GameObj playerObj = new GameObj("green", ObjectType.Player, pos, Vector3Int.forward, prefabKey);
        gameState.PlaceObjectAt(playerObj, pos);
        animationControl.RegisterObject(playerObj, model.transform);

        geoState.RegisterSpawnPoint(prefabKey, pos + Vector3Int.down);
    }

    /// <summary>Spawn a 1x1 box, register with GameStateV2 and AnimationControlV2.</summary>
    private void SpawnBox(Vector3Int pos, string color)
    {
        int        keyIndex   = ColorToPrefabKeyIndex(color);
        int        modelIndex = ColorToBoxModelIndex(color);
        GameObject prefabKey  = GetPrefabKeyByIndex(keyIndex);

        GameObject model = modelIndex >= 0 && modelIndex < boxModels.Length && boxModels[modelIndex] != null
            ? Instantiate(boxModels[modelIndex], (Vector3)pos, Quaternion.identity, levelRoot)
            : CreateFallbackCube(pos, $"Box_{color}");

        model.name           = $"Box_{color}";
        model.transform.localScale = Vector3.one; // ensure clean scale on spawn

        GameObj boxObj = new GameObj(color, ObjectType.Box, pos, Vector3Int.forward, prefabKey);
        gameState.PlaceObjectAt(boxObj, pos);
        animationControl.RegisterObject(boxObj, model.transform);

        geoState.RegisterSpawnPoint(prefabKey, pos + Vector3Int.down);
    }

    /// <summary>Spawn a 1x2 box, register with GameStateV2 and AnimationControlV2.</summary>
    private void SpawnLargeBox(Vector3Int pos, string color, Vector3Int rotation)
    {
        int        keyIndex    = ColorToPrefabKeyIndex(color);
        int        modelIndex  = ColorToBoxModelIndex(color);
        GameObject prefabKey   = GetPrefabKeyByIndex(keyIndex);

        // Parent sits at the primary cell. AnimationControl drives this transform;
        // both child meshes move with it automatically.
        GameObject parent = new GameObject($"LargeBox_{color}");
        parent.transform.SetParent(levelRoot);
        parent.transform.position   = (Vector3)pos;
        parent.transform.localScale = Vector3.one; // ensure clean scale on spawn

        if (modelIndex >= 0 && modelIndex < boxModels.Length && boxModels[modelIndex] != null)
        {
            Vector3Int secondary = pos - rotation;
            Instantiate(boxModels[modelIndex], (Vector3)pos,       Quaternion.identity, parent.transform);
            Instantiate(boxModels[modelIndex], (Vector3)secondary,  Quaternion.identity, parent.transform);
        }

        GameObj boxObj = new GameObj(color, ObjectType.LargeBox, pos, rotation, prefabKey);
        gameState.PlaceObjectAt(boxObj, pos);
        animationControl.RegisterObject(boxObj, parent.transform);

        geoState.RegisterSpawnPoint(prefabKey, pos + Vector3Int.down);
    }

    // ── Geo Visual Spawning ───────────────────────────────────────────────────

    private Transform levelRoot;

    private void ClearVisuals()
    {
        // Unregister all objects from AnimationControlV2 BEFORE destroying their
        // GameObjects, so no stale Transform references remain.
        if (animationControl != null)
            animationControl.ClearAllRegistrations();

        if (levelRoot != null) Destroy(levelRoot.gameObject);
        levelRoot = new GameObject("LevelVisuals").transform;
    }

    private void SpawnGeoVisual(Vector3Int pos, GeoType geoType, int tileId)
    {
        int        colorIndex    = tileId > 0 ? (tileId - 1) % 4 : 0;
        GameObject prefabToSpawn = baseBlockModel;

        if      (geoType == GeoType.Spawn && spawnModels[colorIndex] != null) prefabToSpawn = spawnModels[colorIndex];
        else if (geoType == GeoType.Exit  && exitModels[colorIndex]  != null) prefabToSpawn = exitModels[colorIndex];

        GameObject visual = Instantiate(prefabToSpawn, (Vector3)pos, Quaternion.identity, levelRoot);
        visual.name = $"Geo_{geoType}_{pos}";
    }


    // ── Camera ────────────────────────────────────────────────────────────────

    private void ApplyCamera(LevelRoot levelData)
    {
        if (cameraController == null) return;

        Vector3 origin       = CalculateLevelCenter(levelData.tiles);
        Vector3 offset       = Vector3.zero;
        float   zoom         = 5f;
        float   yaw          = 0f;
        float   pitch        = -45f;

        if (levelData.camera != null)
        {
            if (levelData.camera.origin != null) origin = levelData.camera.origin.ToVector3();
            if (levelData.camera.offset != null) offset = levelData.camera.offset.ToVector3();
            zoom  = levelData.camera.zoom;
            yaw   = levelData.camera.yawRotation;
            pitch = levelData.camera.pitchRotation;
        }

        cameraController.SetCameraData(new CameraData
        {
            origin        = origin,
            offset        = offset,
            zoom          = zoom,
            yawRotation   = yaw,
            pitchRotation = pitch
        });
    }

    private Vector3 CalculateLevelCenter(TileEntry[] tiles)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        bool  any  = false;

        foreach (TileEntry entry in tiles)
        {
            if (entry?.position == null || entry.tile_id != 0) continue;
            any  = true;
            minX = Mathf.Min(minX, entry.position.x); maxX = Mathf.Max(maxX, entry.position.x);
            minY = Mathf.Min(minY, entry.position.y); maxY = Mathf.Max(maxY, entry.position.y);
            minZ = Mathf.Min(minZ, entry.position.z); maxZ = Mathf.Max(maxZ, entry.position.z);
        }

        return any ? new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f)
                   : Vector3.zero;
    }

    // ── Prefab Key Helpers ────────────────────────────────────────────────────

    private void CreatePrefabKeys()
    {
        greenPrefabKey  = CreateKey("PrefabKey_Green");
        redPrefabKey    = CreateKey("PrefabKey_Red");
        bluePrefabKey   = CreateKey("PrefabKey_Blue");
        yellowPrefabKey = CreateKey("PrefabKey_Yellow");
    }

    private GameObject CreateKey(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.SetActive(false);
        return obj;
    }

    public GameObject GetPrefabKey(string color)
    {
        return color.ToLower() switch
        {
            "green"  => greenPrefabKey,
            "red"    => redPrefabKey,
            "blue"   => bluePrefabKey,
            "yellow" => yellowPrefabKey,
            _        => null
        };
    }

    private GameObject GetPrefabKeyByIndex(int index)
    {
        return index switch { 0 => greenPrefabKey, 1 => redPrefabKey, 2 => bluePrefabKey, 3 => yellowPrefabKey, _ => null };
    }

    // ── Misc Helpers ──────────────────────────────────────────────────────────

    private Vector3Int GetPos(TileEntry entry) =>
        new Vector3Int(entry.position.x, entry.position.y, entry.position.z);

    /// <summary>Returns the prefab key index for a color name (green=0, red=1, blue=2, yellow=3).</summary>
    private int ColorToPrefabKeyIndex(string color) => color.ToLower() switch
    {
        "green"  => 0,
        "red"    => 1,
        "blue"   => 2,
        "yellow" => 3,
        _        => -1
    };

    /// <summary>Returns the boxModels array index for a color name (red=0, blue=1, yellow=2). Green has no box model — it is always the player.</summary>
    private int ColorToBoxModelIndex(string color) => color.ToLower() switch
    {
        "red"    => 0,
        "blue"   => 1,
        "yellow" => 2,
        _        => -1   // green or unknown — no box model slot
    };

    private int FindAdjacentPair(List<Vector3Int> positions, int i, HashSet<int> used)
    {
        for (int j = i + 1; j < positions.Count; j++)
        {
            if (used.Contains(j)) continue;
            Vector3Int diff = positions[j] - positions[i];
            if (diff.y == 0 && Mathf.Abs(diff.x) + Mathf.Abs(diff.z) == 1)
                return j;
        }
        return -1;
    }

    private GameObject CreateFallbackCube(Vector3Int pos, string name)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name                  = name;
        cube.transform.position    = (Vector3)pos;
        cube.transform.SetParent(levelRoot);
        return cube;
    }
}

} // namespace NewArch
