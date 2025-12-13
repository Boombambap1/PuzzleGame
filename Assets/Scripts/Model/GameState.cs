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
    
    // Win conditions (can be set per level)
    private Dictionary<string, List<Vector3Int>> winConditions;
    
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
            winConditions = new Dictionary<string, List<Vector3Int>>();
        }
    }
    
    /// <summary>
    /// Place an object at a position
    /// </summary>
    public void PlaceObjectAt(Object obj, Vector3Int pos)
    {
        InitializeDictionaries(); // Ensure dictionaries exist
        
        // Remove from old position if it exists
        if (objectGrid.ContainsValue(obj))
        {
            RemoveObjectAt(obj.position);
        }
        
        obj.position = pos;
        objectGrid[pos] = obj;
        
        if (!allObjects.Contains(obj))
        {
            allObjects.Add(obj);
        }
        
        // Track player
        if (obj.type == "robot")
        {
            player = obj;
        }
    }
    
    /// <summary>
    /// Remove object at a position
    /// </summary>
    public void RemoveObjectAt(Vector3Int pos)
    {
        if (objectGrid.TryGetValue(pos, out Object obj))
        {
            objectGrid.Remove(pos);
            allObjects.Remove(obj);
            
            if (obj == player)
            {
                player = null;
            }
        }
    }
    
    /// <summary>
    /// Move an object to a new position
    /// </summary>
    public void MoveObjectTo(Object obj, Vector3Int newPos)
    {
        // Remove from old position
        if (objectGrid.ContainsKey(obj.position))
        {
            objectGrid.Remove(obj.position);
        }
        
        // Update position
        obj.position = newPos;
        
        // Place at new position
        objectGrid[newPos] = obj;
    }
    
    /// <summary>
    /// Clear all objects from the game
    /// </summary>
    public void ClearAllObjects()
    {
        objectGrid.Clear();
        allObjects.Clear();
        player = null;
    }
    
    /// <summary>
    /// Get object at a specific position
    /// </summary>
    public Object GetObjectAt(Vector3Int pos)
    {
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
        
        Vector3Int belowPos = obj.position + Vector3Int.down;
        
        // Check if there's ground (geo) below
        GeoType geoBelow = geoState.GetGeoTypeAt(belowPos);
        if (geoBelow == GeoType.Block)
        {
            return false; // Standing on solid ground
        }
        
        // Check if there's an object below
        Object objBelow = GetObjectAt(belowPos);
        if (objBelow != null && objBelow.IsAlive())
        {
            return false; // Standing on another object
        }
        
        return true; // Nothing below, in freefall
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
    /// Set win conditions for a level
    /// Example: green boxes must be at specific positions
    /// </summary>
    public void SetWinCondition(string color, List<Vector3Int> requiredPositions)
    {
        winConditions[color] = requiredPositions;
    }
    
    /// <summary>
    /// Check if the current state is a winning state
    /// </summary>
    public bool IsWinningState()
    {
        // Check if player is at an exit
        if (player != null && player.IsAlive())
        {
            GeoType playerGeo = geoState.GetGeoTypeAt(player.position);
            if (playerGeo == GeoType.Exit)
            {
                // If there are no other win conditions, player reaching exit is enough
                if (winConditions.Count == 0)
                {
                    return true;
                }
            }
        }
        
        // Check color-based win conditions
        foreach (var condition in winConditions)
        {
            string color = condition.Key;
            List<Vector3Int> requiredPositions = condition.Value;
            
            // Find all objects of this color
            List<Object> colorObjects = allObjects.FindAll(obj => 
                obj.color == color && obj.IsAlive());
            
            // Check if all required positions have the correct colored object
            foreach (Vector3Int pos in requiredPositions)
            {
                Object objAtPos = GetObjectAt(pos);
                if (objAtPos == null || objAtPos.color != color || !objAtPos.IsAlive())
                {
                    return false;
                }
            }
        }
        
        return winConditions.Count > 0; // Only return true if there were conditions to check
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


