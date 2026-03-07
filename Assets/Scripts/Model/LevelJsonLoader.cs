using System.Collections.Generic;
using UnityEngine;

public class LevelJsonLoader : MonoBehaviour
{
    private GeoState geoState;
    private GameState gameState;
    private CameraController cameraController;

    // Auto-generated placeholder objects used as identity keys for each color
    private GameObject greenBoxPrefab;
    private GameObject redBoxPrefab;
    private GameObject blueBoxPrefab;
    private GameObject yellowBoxPrefab;

    // Parent for all spawned visual blocks
    private Transform levelRoot;

    private static readonly string[] colorNames = { "green", "red", "blue", "yellow" };

    [Header("Geo Models")]
    [SerializeField] private GameObject baseBlockModel;
    [SerializeField] private GameObject[] spawnModels = new GameObject[4];
    [SerializeField] private GameObject[] exitModels = new GameObject[4];

    [Header("Object Models")]
    [SerializeField] private GameObject[] boxModels = new GameObject[3];
    [SerializeField] private GameObject playerModel;

    [System.Serializable]
    private class LevelTilePosition
    {
        public int x;
        public int y;
        public int z;
    }

    [System.Serializable]
    private class LevelTileEntry
    {
        public LevelTilePosition position;
        public int tile_id;
    }

    [System.Serializable]
    private class SerializableVec3
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    private class LevelCameraSettings
    {
        public SerializableVec3 origin;
        public SerializableVec3 offset;
        public float zoom = 5f;
        public float yawRotation = 0f;
        public float pitchRotation = -45f;
    }

    [System.Serializable]
    private class LevelJsonRoot
    {
        public LevelTileEntry[] tiles;
        public LevelCameraSettings camera;
    }

    void Awake()
    {
        geoState = FindObjectOfType<GeoState>();
        gameState = FindObjectOfType<GameState>();
        cameraController = FindObjectOfType<CameraController>();

        greenBoxPrefab = CreateColorKey("GreenBox");
        redBoxPrefab = CreateColorKey("RedBox");
        blueBoxPrefab = CreateColorKey("BlueBox");
        yellowBoxPrefab = CreateColorKey("YellowBox");
    }

    public GameObject GetColorPrefab(string color)
    {
        switch (color.ToLower())
        {
            case "green": return greenBoxPrefab;
            case "red": return redBoxPrefab;
            case "blue": return blueBoxPrefab;
            case "yellow": return yellowBoxPrefab;
            default: return null;
        }
    }

    public void LoadLevelFromJson(string json)
    {
        geoState.ClearAllGeo();
        gameState.ClearAllObjects();
        ClearVisuals();

        string trimmed = json.Trim();
        string wrapped = trimmed.StartsWith("[") ? "{\"tiles\":" + trimmed + "}" : trimmed;

        LevelJsonRoot levelData = JsonUtility.FromJson<LevelJsonRoot>(wrapped);
        if (levelData == null || levelData.tiles == null)
        {
            Debug.LogError("failed to parse json");
            return;
        }

        // Separate and process tiles by type
        Dictionary<Vector3Int, (GeoType type, int tileId)> geoColorOverrides = new Dictionary<Vector3Int, (GeoType, int)>();
        Dictionary<int, List<Vector3Int>> spawnGroups = new Dictionary<int, List<Vector3Int>>();
        List<LevelTileEntry> geoEntries = new List<LevelTileEntry>();

        foreach (LevelTileEntry entry in levelData.tiles)
        {
            if (entry == null || entry.position == null) continue;

            if (entry.tile_id == 0)
            {
                geoEntries.Add(entry);
            }
            else if (entry.tile_id >= 1 && entry.tile_id <= 8)
            {
                Vector3Int pos = GetPosition(entry);
                Vector3Int below = pos + Vector3Int.down;
                GameObject prefab = GetColorPrefabByIndex((entry.tile_id - 1) % 4);

                if (entry.tile_id <= 4)
                {
                    // Spawn tile
                    geoColorOverrides[below] = (GeoType.Spawn, entry.tile_id);
                    if (prefab != null) geoState.RegisterSpawnPoint(prefab, below);

                    if (!spawnGroups.ContainsKey(entry.tile_id))
                        spawnGroups[entry.tile_id] = new List<Vector3Int>();
                    spawnGroups[entry.tile_id].Add(pos);
                }
                else
                {
                    // Exit tile
                    geoColorOverrides[below] = (GeoType.Exit, entry.tile_id);
                    if (prefab != null) gameState.RegisterWinCondition(prefab, below);
                }
            }
            else
            {
                Debug.LogWarning($"unknown tile_id {entry.tile_id}");
            }
        }

        // Place geo blocks with appropriate colors
        foreach (LevelTileEntry entry in geoEntries)
        {
            Vector3Int pos = GetPosition(entry);

            if (geoColorOverrides.TryGetValue(pos, out var colorInfo))
            {
                geoState.PlaceGeoAt(pos, colorInfo.type);
                SpawnVisualBlock(pos, colorInfo.type, colorInfo.tileId);
            }
            else
            {
                geoState.PlaceGeoAt(pos, GeoType.Block);
                SpawnVisualBlock(pos, GeoType.Block, 0);
            }
        }

        // Spawn player and box objects with 1x2 detection
        foreach (var kvp in spawnGroups)
        {
            SpawnObjectsFromGroup(kvp.Key, kvp.Value);
        }

        ApplyCameraFromLevelData(levelData);
    }

    private void ApplyCameraFromLevelData(LevelJsonRoot levelData)
    {
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (cameraController == null)
        {
            return;
        }

        Vector3 centeredOrigin = CalculateCenteredOrigin(levelData.tiles);

        Vector3 origin = centeredOrigin;
        Vector3 offset = Vector3.zero;
        float zoom = 5f;
        float yawRotation = 0f;
        float pitchRotation = -45f;

        if (levelData.camera != null)
        {
            if (levelData.camera.origin != null)
                origin = levelData.camera.origin.ToVector3();
            if (levelData.camera.offset != null)
                offset = levelData.camera.offset.ToVector3();

            zoom = levelData.camera.zoom;
            yawRotation = levelData.camera.yawRotation;
            pitchRotation = levelData.camera.pitchRotation;
        }

        CameraData data = new CameraData
        {
            origin = origin,
            offset = offset,
            zoom = zoom,
            yawRotation = yawRotation,
            pitchRotation = pitchRotation
        };

        cameraController.SetCameraData(data);
    }

    private Vector3 CalculateCenteredOrigin(LevelTileEntry[] tiles)
    {
        if (tiles == null || tiles.Length == 0)
        {
            return Vector3.zero;
        }

        bool hasGeoTile = false;
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        foreach (LevelTileEntry entry in tiles)
        {
            if (entry == null || entry.position == null)
            {
                continue;
            }

            if (entry.tile_id != 0)
            {
                continue;
            }

            hasGeoTile = true;
            minX = Mathf.Min(minX, entry.position.x);
            minY = Mathf.Min(minY, entry.position.y);
            minZ = Mathf.Min(minZ, entry.position.z);
            maxX = Mathf.Max(maxX, entry.position.x);
            maxY = Mathf.Max(maxY, entry.position.y);
            maxZ = Mathf.Max(maxZ, entry.position.z);
        }

        if (!hasGeoTile)
        {
            return Vector3.zero;
        }

        return new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f
        );
    }

    private Vector3Int GetPosition(LevelTileEntry entry)
    {
        return new Vector3Int(entry.position.x, entry.position.y, entry.position.z);
    }

    private void SpawnObjectsFromGroup(int tileId, List<Vector3Int> positions)
    {
        int colorIndex = (tileId - 1) % 4;
        string color = colorNames[colorIndex];
        HashSet<int> used = new HashSet<int>();

        for (int i = 0; i < positions.Count; i++)
        {
            if (used.Contains(i)) continue;

            // Check for adjacent pair to form 1x2 box
            int pairedIndex = FindAdjacentPair(positions, i, used);

            if (pairedIndex >= 0)
            {
                Vector3Int rotation = positions[pairedIndex] - positions[i];
                used.Add(i);
                used.Add(pairedIndex);
                SpawnBox1x2(positions[i], color, -rotation);
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

    private int FindAdjacentPair(List<Vector3Int> positions, int currentIndex, HashSet<int> used)
    {
        for (int j = currentIndex + 1; j < positions.Count; j++)
        {
            if (used.Contains(j)) continue;
            Vector3Int diff = positions[j] - positions[currentIndex];
            if (IsAdjacent(diff))
                return j;
        }
        return -1;
    }

    private bool IsAdjacent(Vector3Int diff)
    {
        // Check if exactly one tile away horizontally (x or z axis, not y)
        return diff.y == 0 && Mathf.Abs(diff.x) + Mathf.Abs(diff.z) == 1;
    }

    private GameObject GetColorPrefabByIndex(int colorIndex)
    {
        GameObject[] prefabs = { greenBoxPrefab, redBoxPrefab, blueBoxPrefab, yellowBoxPrefab };
        if (colorIndex >= 0 && colorIndex < prefabs.Length) return prefabs[colorIndex];
        return null;
    }

    private GameObject CreateColorKey(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.SetActive(false);
        return obj;
    }

    private void ClearVisuals()
    {
        if (levelRoot != null)
        {
            Destroy(levelRoot.gameObject);
        }
        GameObject root = new GameObject("LevelVisuals");
        levelRoot = root.transform;
    }

    private void SpawnVisualBlock(Vector3Int pos, GeoType geoType, int tileId)
    {
        GameObject prefabToSpawn = baseBlockModel;
        int colorIndex = (tileId - 1) % 4;
        if (geoType == GeoType.Spawn) prefabToSpawn = spawnModels[colorIndex];
        else if (geoType == GeoType.Exit) prefabToSpawn = exitModels[colorIndex];

        GameObject visualBlock = Instantiate(prefabToSpawn, (Vector3)pos, Quaternion.identity);
        visualBlock.name = $"Tile_{tileId}_{pos}";
        visualBlock.transform.SetParent(levelRoot);
    }

    private void SpawnPlayer(Vector3Int pos)
    {
        GameObject prefabToSpawn = playerModel != null ? playerModel : GameObject.CreatePrimitive(PrimitiveType.Cube);

        GameObject playerObj = Instantiate(prefabToSpawn, (Vector3)pos, Quaternion.identity);
        playerObj.name = "Player";
        playerObj.transform.SetParent(levelRoot);

        PlayerInput playerInput = playerObj.AddComponent<PlayerInput>();
        playerInput.prefabReference = greenBoxPrefab;
        playerInput.moveSpeed = 10f;
    }

    private void SpawnBox(Vector3Int pos, string color)
    {
        int colorIndex = -1;
        switch (color.ToLower())
        {
            case "red": colorIndex = 0; break;
            case "blue": colorIndex = 1; break;
            case "yellow": colorIndex = 2; break;
        }

        GameObject prefabToSpawn = boxModels[colorIndex];

        GameObject boxObj = Instantiate(prefabToSpawn, (Vector3)pos, Quaternion.identity);
        boxObj.name = $"Box_{color}";
        boxObj.transform.SetParent(levelRoot);

        PushBlocks pushBlocks = boxObj.AddComponent<PushBlocks>();
        pushBlocks.boxColor = color;
        pushBlocks.boxType = "box";
        pushBlocks.prefabReference = GetColorPrefab(color);
        pushBlocks.moveSpeed = 10f;
    }

    private void SpawnBox1x2(Vector3Int mainPos, string color, Vector3Int rotation)
    {
        GameObject boxObj = new GameObject($"Box1x2_{color}");
        boxObj.transform.SetParent(levelRoot);
        boxObj.transform.position = mainPos - rotation;

        int colorIndex = -1;
        switch (color.ToLower())
        {
            case "red": colorIndex = 0; break;
            case "blue": colorIndex = 1; break;
            case "yellow": colorIndex = 2; break;
        }

        GameObject prefabToSpawn = boxModels[colorIndex];

        Instantiate(prefabToSpawn, (Vector3)mainPos, Quaternion.identity, boxObj.transform);
        Instantiate(prefabToSpawn, (Vector3)(mainPos - rotation), Quaternion.identity, boxObj.transform);

        PushBlocks pushBlocks = boxObj.AddComponent<PushBlocks>();
        pushBlocks.boxColor = color;
        pushBlocks.boxType = "1x2_box";
        pushBlocks.boxRotation = rotation;
        pushBlocks.prefabReference = GetColorPrefab(color);
        pushBlocks.moveSpeed = 10f;
    }
}
