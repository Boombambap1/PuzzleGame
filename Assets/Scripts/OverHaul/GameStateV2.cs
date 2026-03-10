using System.Collections.Generic;
using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Stores all movable game objects and provides spatial queries.
/// Pure state container — no physics logic lives here.
/// Part of the Model layer (MVC).
/// </summary>
public class GameStateV2 : MonoBehaviour
{
    // ── Dependencies ──────────────────────────────────────────────────────────

    private GeoStateV2 geoState;

    // ── Private State ─────────────────────────────────────────────────────────

    private Dictionary<Vector3Int, GameObj>          objectGrid    = new();
    private List<GameObj>                            allObjects    = new();
    private GameObj                                  player;
    private Dictionary<GameObject, List<Vector3Int>> winConditions = new();

    [SerializeField] private WinMenu winMenu;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        geoState = GetComponent<GeoStateV2>();
        if (geoState == null)
            Debug.LogError("[GameStateV2] GeoStateV2 component not found on the same GameObject.");
    }

    // ── Object Placement ──────────────────────────────────────────────────────

    public void PlaceObjectAt(GameObj obj, Vector3Int pos)
    {
        if (allObjects.Contains(obj))
            RemoveFromGrid(obj);

        obj.position = pos;
        WriteToGrid(obj);

        if (!allObjects.Contains(obj))
            allObjects.Add(obj);

        if (obj.type == ObjectType.Player)
            player = obj;
    }

    public void RemoveObjectAt(Vector3Int pos)
    {
        if (objectGrid.TryGetValue(pos, out GameObj obj))
            RemoveFromGrid(obj);
    }

    public void MoveObjectTo(GameObj obj, Vector3Int newPos)
    {
        RemoveFromGrid(obj);
        obj.position = newPos;
        WriteToGrid(obj);
    }

    public void ClearAllObjects()
    {
        objectGrid.Clear();
        allObjects.Clear();
        winConditions.Clear();
        player = null;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public GameObj        GetObjectAt(Vector3Int pos)    { objectGrid.TryGetValue(pos, out GameObj obj); return obj; }
    public string         GetObjectColorAt(Vector3Int pos) => GetObjectAt(pos)?.color ?? "none";
    public Vector3Int     GetObjectPos(GameObj obj)      => obj.position;
    public string         GetObjectColor(GameObj obj)    => obj.color;
    public bool           IsObjectAlive(GameObj obj)     => obj.IsAlive();
    public List<GameObj>  GetAllObjects()                => new List<GameObj>(allObjects);
    public GameObj        GetPlayer()                    => player;

    // ── Physics Queries ───────────────────────────────────────────────────────

    public bool IsObjectInFreefall(GameObj obj)
    {
        if (!obj.IsAlive()) return false;

        foreach (Vector3Int cell in obj.GetOccupiedPositions())
        {
            Vector3Int below    = cell + Vector3Int.down;
            GeoType    geoBelow = geoState.GetGeoTypeAt(below);

            if (geoBelow == GeoType.Block || geoBelow == GeoType.Exit || geoBelow == GeoType.Spawn)
                return false;

            GameObj objBelow = GetObjectAt(below);
            if (objBelow != null && objBelow.IsAlive())
                return false;
        }

        return true;
    }

    public bool IsObjectOnGround(GameObj obj) => !IsObjectInFreefall(obj);

    // ── Win Condition ─────────────────────────────────────────────────────────

    public void RegisterWinCondition(GameObject prefab, Vector3Int exitPos)
    {
        if (!winConditions.ContainsKey(prefab))
            winConditions[prefab] = new List<Vector3Int>();
        winConditions[prefab].Add(exitPos);
    }

    public bool IsWinningState()
    {
        if (winConditions.Count == 0) return false;

        foreach (KeyValuePair<GameObject, List<Vector3Int>> entry in winConditions)
        {
            foreach (Vector3Int exitPos in entry.Value)
            {
                GameObj obj = GetObjectAt(exitPos + Vector3Int.up);
                if (obj == null || !obj.IsAlive() || obj.prefab != entry.Key)
                    return false;
            }
        }

        winMenu?.LevelComplete();
        return true;
    }

    public Dictionary<GameObject, List<Vector3Int>> GetWinConditions() =>
        new Dictionary<GameObject, List<Vector3Int>>(winConditions);

    // ── Level Loading ─────────────────────────────────────────────────────────

    public void LoadObjects(List<GameObj> objects)
    {
        ClearAllObjects();
        foreach (GameObj obj in objects)
            PlaceObjectAt(obj, obj.position);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void WriteToGrid(GameObj obj)
    {
        foreach (Vector3Int cell in obj.GetOccupiedPositions())
            objectGrid[cell] = obj;
    }

    private void RemoveFromGrid(GameObj obj)
    {
        foreach (Vector3Int cell in obj.GetOccupiedPositions())
            if (objectGrid.TryGetValue(cell, out GameObj stored) && stored == obj)
                objectGrid.Remove(cell);
    }
}

} // namespace NewArch
