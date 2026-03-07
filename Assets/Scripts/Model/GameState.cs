using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    // Object storage
    private Dictionary<Vector3Int, Object> objectGrid;
    private List<Object> allObjects;
    private Object player;

    // Reference to GeoState for ground checks
    private GeoState geoState;

    // Win conditions (can be set per level) - NOW USES PREFABS
    private Dictionary<GameObject, List<Vector3Int>> winConditions;

    [SerializeField] private WinMenu winMenu;

    void Awake()
    {
        InitializeDictionaries();
        geoState = GetComponent<GeoState>();

        if (geoState == null)
        {
            Debug.LogError("GameState: GeoState component not found!");
        }
    }

    private void InitializeDictionaries()
    {
        if (objectGrid == null)
        {
            objectGrid = new Dictionary<Vector3Int, Object>();
        }
        if (allObjects == null)
        {
            allObjects = new List<Object>();
        }
        if (winConditions == null)
        {
            winConditions = new Dictionary<GameObject, List<Vector3Int>>();
        }
    }

    private Vector3Int[] GetOccupiedPositions(Object obj)
    {
        if (obj == null)
        {
            return new Vector3Int[0];
        }

        Vector3Int secondary = obj.GetSecondaryPosition();
        if (obj.type == "1x2_box")
        {
            return new[] { obj.position, secondary };
        }

        return new[] { obj.position };
    }

    /// <summary>
    /// Place an object at a position
    /// </summary>
    public void PlaceObjectAt(Object obj, Vector3Int pos)
    {
        InitializeDictionaries();

        if (objectGrid.ContainsValue(obj))
        {
            RemoveObjectFromGrid(obj.position);
        }

        obj.position = pos;
        foreach (Vector3Int occupiedPos in GetOccupiedPositions(obj))
        {
            objectGrid[occupiedPos] = obj;
        }

        if (!allObjects.Contains(obj))
        {
            allObjects.Add(obj);
        }

        if (obj.type == "player")
        {
            player = obj;
        }
    }

    /// <summary>
    /// Remove object from grid only (keeps it in allObjects for respawn tracking)
    /// </summary>
    public void RemoveObjectFromGrid(Vector3Int pos)
    {
        InitializeDictionaries();
        
        if (objectGrid.TryGetValue(pos, out Object obj))
        {
            foreach (Vector3Int occupiedPos in GetOccupiedPositions(obj))
            {
                objectGrid.Remove(occupiedPos);
            }
            // NOTE: intentionally NOT removing from allObjects or nulling player
        }
    }

    /// <summary>
    /// Move an object to a new position
    /// </summary>
    public void MoveObjectTo(Object obj, Vector3Int newPos)
    {
        foreach (Vector3Int occupiedPos in GetOccupiedPositions(obj))
        {
            objectGrid.Remove(occupiedPos);
        }

        obj.position = newPos;
        foreach (Vector3Int occupiedPos in GetOccupiedPositions(obj))
        {
            objectGrid[occupiedPos] = obj;
        }
    }

    /// <summary>
    /// Clear all objects from the game
    /// </summary>
    public void ClearAllObjects()
    {
        objectGrid.Clear();
        allObjects.Clear();
        player = null;
        winConditions.Clear();
    }

    /// <summary>
    /// Get object at a specific position
    /// </summary>
    public Object GetObjectAt(Vector3Int pos)
    {
        InitializeDictionaries();
        objectGrid.TryGetValue(pos, out Object obj);
        return obj;
    }

    /// <summary>
    /// Get the color of object at a position
    /// </summary>
    public string GetObjectColorAt(Vector3Int pos)
    {
        Object obj = GetObjectAt(pos);
        return obj?.color ?? "none";
    }

    /// <summary>
    /// Get the position of an object
    /// </summary>
    public Vector3Int GetObjectPos(Object obj)
    {
        return obj.position;
    }

    /// <summary>
    /// Get the color of an object
    /// </summary>
    public string GetObjectColor(Object obj)
    {
        return obj.color;
    }

    /// <summary>
    /// Check if object is in freefall (no support below)
    /// </summary>
    public bool IsObjectInFreefall(Object obj)
    {
        if (!obj.IsAlive()) return false;

        if (geoState == null)
        {
            Debug.LogError("GameState: geoState is null in IsObjectInFreefall!");
            return false;
        }

        foreach (Vector3Int occupiedPos in GetOccupiedPositions(obj))
        {
            Vector3Int belowPos = occupiedPos + Vector3Int.down;


            GeoType geoBelow = geoState.GetGeoTypeAt(belowPos);

            if (geoBelow == GeoType.Block || geoBelow == GeoType.Exit || geoBelow == GeoType.Spawn)
            {
                return false;
            }

            Object objBelow = GetObjectAt(belowPos);
            if (objBelow != null && objBelow.IsAlive())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if object is on ground
    /// </summary>
    public bool IsObjectOnGround(Object obj)
    {
        return !IsObjectInFreefall(obj);
    }

    /// <summary>
    /// Check if object is alive
    /// </summary>
    public bool IsObjectAlive(Object obj)
    {
        return obj.alive;
    }

    /// <summary>
    /// Get all objects in the game
    /// </summary>
    public List<Object> GetAllObjects()
    {
        return new List<Object>(allObjects);
    }

    /// <summary>
    /// Get the player object
    /// </summary>
    public Object GetPlayer()
    {
        return player;
    }

    /// <summary>
    /// Register a win condition: this prefab must reach this position
    /// Called by GeoBlocks with Exit type
    /// </summary>
    public void RegisterWinCondition(GameObject prefab, Vector3Int position)
    {
        InitializeDictionaries();

        if (!winConditions.ContainsKey(prefab))
        {
            winConditions[prefab] = new List<Vector3Int>();
        }

        winConditions[prefab].Add(position);
    }

    /// <summary>
    /// Check if all win conditions are satisfied
    /// Returns true only if ALL required objects are at their correct exit positions
    /// </summary>
    public bool CheckWinConditions()
    {
        InitializeDictionaries();

        if (winConditions.Count == 0)
        {
            return false;
        }


        foreach (var kvp in winConditions)
        {
            GameObject requiredPrefab = kvp.Key;
            List<Vector3Int> requiredPositions = kvp.Value;


            foreach (Vector3Int exitPos in requiredPositions)
            {
                // Check the position ABOVE the exit block (where the object would be standing)
                Vector3Int checkPos = exitPos + Vector3Int.up;
                Object objAtPosition = GetObjectAt(checkPos);

                if (objAtPosition == null)
                {
                    return false;
                }

                if (!objAtPosition.IsAlive())
                {
                    return false;
                }

                // Check if it matches the required prefab
                if (objAtPosition.prefab != requiredPrefab)
                {
                    string actualPrefab = objAtPosition.prefab != null ? objAtPosition.prefab.name : "NULL";

                    return false;
                }

            }
        }

        Debug.Log("========================================");
        Debug.Log("[WinCheck] ✓✓✓ ALL WIN CONDITIONS SATISFIED! ✓✓✓");
        Debug.Log("========================================");

        winMenu.LevelComplete();
        return true;
    }

    /// <summary>
    /// Get all win condition positions (for debugging/visualization)
    /// </summary>
    public Dictionary<GameObject, List<Vector3Int>> GetWinConditions()
    {
        InitializeDictionaries();
        return winConditions;
    }

    /// <summary>
    /// Load initial objects for a level
    /// </summary>
    public void LoadObjects(List<Object> objects)
    {
        ClearAllObjects();

        foreach (Object obj in objects)
        {
            PlaceObjectAt(obj, obj.position);
        }
    }
}


