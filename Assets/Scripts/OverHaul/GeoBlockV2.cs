using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Attach to any GameObject to register it as a geo tile in GeoStateV2.
/// Each block occupies exactly one 1x1x1 grid cell at its rounded position.
///
/// For Spawn tiles: calls GeoStateV2.RegisterSpawnPoint() so the physics
/// system can find this position when respawning objects.
///
/// For Exit tiles: calls GameStateV2.RegisterWinCondition().
///
/// Drop-in replacement for GeoBlock — old GeoBlock is untouched.
/// </summary>
public class GeoBlockV2 : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Block Settings")]
    [Tooltip("Type of geo tile this block represents.")]
    public GeoType blockType = GeoType.Block;

    [Tooltip("Exit blocks: the prefab identity key that must reach this exit.")]
    public GameObject exitPrefab;

    [Tooltip("Spawn blocks: the prefab identity key that spawns here.")]
    public GameObject spawnPrefab;

    [Header("Debug")]
    public bool  showGizmo  = true;
    public Color gizmoColor = Color.white;

    // ── Private State ─────────────────────────────────────────────────────────

    private GeoStateV2  geoState;
    private GameStateV2 gameState;
    private Vector3Int  occupiedPosition;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        geoState  = FindFirstObjectByType<GeoStateV2>();
        gameState = FindFirstObjectByType<GameStateV2>();

        if (geoState == null)
        {
            Debug.LogError($"[GeoBlockV2] '{gameObject.name}': GeoStateV2 not found in scene.");
            return;
        }

        RegisterBlock();
    }

    void OnDestroy()
    {
        geoState?.RemoveGeoAt(occupiedPosition);
    }

    // ── Registration ──────────────────────────────────────────────────────────

    private void RegisterBlock()
    {
        occupiedPosition = Vector3Int.RoundToInt(transform.position);
        geoState.PlaceGeoAt(occupiedPosition, blockType);

        switch (blockType)
        {
            case GeoType.Exit:
                RegisterExit();
                break;

            case GeoType.Spawn:
                RegisterSpawn();
                break;

            default:
                Debug.Log($"[GeoBlockV2] '{gameObject.name}': Registered {blockType} at {occupiedPosition}.");
                break;
        }
    }

    private void RegisterExit()
    {
        if (gameState == null)
        {
            Debug.LogWarning($"[GeoBlockV2] '{gameObject.name}': GameStateV2 not found — cannot register exit.");
            return;
        }
        if (exitPrefab == null)
        {
            Debug.LogWarning($"[GeoBlockV2] '{gameObject.name}': Exit block has no prefab assigned.");
            return;
        }

        gameState.RegisterWinCondition(exitPrefab, occupiedPosition);
        Debug.Log($"[GeoBlockV2] '{gameObject.name}': Registered Exit for '{exitPrefab.name}' at {occupiedPosition}.");
    }

    private void RegisterSpawn()
    {
        if (spawnPrefab == null)
        {
            Debug.LogWarning($"[GeoBlockV2] '{gameObject.name}': Spawn block has no prefab assigned.");
            return;
        }

        geoState.RegisterSpawnPoint(spawnPrefab, occupiedPosition);
        Debug.Log($"[GeoBlockV2] '{gameObject.name}': Registered Spawn for '{spawnPrefab.name}' at {occupiedPosition}.");
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Color      color   = gizmoColor != Color.white ? gizmoColor : GizmoColorForType();
        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);

        Gizmos.color = color;
        Gizmos.DrawWireCube((Vector3)gridPos, Vector3.one * 0.95f);

        if (blockType == GeoType.Exit || blockType == GeoType.Spawn)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
            Gizmos.DrawCube((Vector3)gridPos, Vector3.one * 0.7f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector3)Vector3Int.RoundToInt(transform.position), Vector3.one);
    }

    private Color GizmoColorForType()
    {
        return blockType switch
        {
            GeoType.Block => Color.white,
            GeoType.Spawn => PrefabToColor(spawnPrefab),
            GeoType.Exit  => PrefabToColor(exitPrefab),
            _             => Color.gray
        };
    }

    private Color PrefabToColor(GameObject prefab)
    {
        if (prefab == null) return Color.green;

        string n = prefab.name.ToLower();
        if (n.Contains("red"))    return Color.red;
        if (n.Contains("blue"))   return Color.blue;
        if (n.Contains("yellow")) return Color.yellow;
        if (n.Contains("green"))  return Color.green;

        return Color.green;
    }
}

} // namespace NewArch
