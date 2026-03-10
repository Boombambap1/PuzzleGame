using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Registers a box with GameStateV2 and AnimationControlV2 on Start.
/// All animation is handled by AnimationControlV2 — this script has no Update loop.
///
/// Drop-in replacement for PushBlocks. Old PushBlocks is untouched.
/// </summary>
public class PushBlocksV2 : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Box Settings")]
    public string     boxColor     = "green";
    public ObjectType boxType      = ObjectType.Box;
    public Vector3Int boxRotation  = Vector3Int.forward;

    [Tooltip("Prefab identity key for respawn matching. Drag the same key GameObject used in GeoBlockV2.")]
    public GameObject prefabReference;

    [Header("Debug")]
    public bool showGizmo = true;

    // ── Private State ─────────────────────────────────────────────────────────

    private GameStateV2        gameState;
    private AnimationControlV2 animationControl;
    public  GameObj            boxObject;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        gameState        = FindFirstObjectByType<GameStateV2>();
        animationControl = FindFirstObjectByType<AnimationControlV2>();

        if (gameState == null)
        {
            Debug.LogError($"[PushBlocksV2] '{gameObject.name}': GameStateV2 not found.");
            return;
        }

        if (animationControl == null)
        {
            Debug.LogError($"[PushBlocksV2] '{gameObject.name}': AnimationControlV2 not found.");
            return;
        }

        if (prefabReference == null)
            Debug.LogWarning($"[PushBlocksV2] '{gameObject.name}': No prefab reference assigned — respawn won't work.");

        Vector3Int gridPos = Vector3Int.RoundToInt(transform.position);

        boxObject = new GameObj(boxColor, boxType, gridPos, boxRotation, prefabReference);
        gameState.PlaceObjectAt(boxObject, gridPos);
        animationControl.RegisterObject(boxObject, transform);
    }

    void OnDestroy()
    {
        if (gameState != null && boxObject != null)
            gameState.RemoveObjectAt(boxObject.position);

        if (animationControl != null && boxObject != null)
            animationControl.UnregisterObject(boxObject);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = boxColor.ToLower() switch
        {
            "red"    => Color.red,
            "blue"   => Color.blue,
            "yellow" => Color.yellow,
            "green"  => Color.green,
            _        => Color.white
        };

        Gizmos.DrawWireCube(Vector3Int.RoundToInt(transform.position), Vector3.one * 0.8f);
    }
}

} // namespace NewArch
