using System.Collections.Generic;
using UnityEngine;

// ============= ENUMS =============

public enum Direction
{
    None,
    Left,
    Right, 
    Forward,
    Backward,
    Up,
    Down
}

public enum TaskAction
{
    Move,      // Standard movement
    Slide,     // Movement that continues until blocked
    Fall,      // Gravity-based downward movement
    Die,       // Object destruction
    Respawn    // Object respawn at spawn point
}

public enum GeoType
{
    Void,      // Empty space
    Block,     // Standard block
    Spawn,     // Player/object spawn point
    Exit       // Win condition
}

public enum InputType
{
    Movement,
    CameraRotation,
    CameraPosition,
    Undo,
    Reset,
    Menu,
    Place,       // Editor
    Delete,      // Editor
    QuickSelect  // Editor
}

// ============= CLASSES =============

/// <summary>
/// Represents all movable objects in the game
/// </summary>
[System.Serializable]
public class Object
{
    // Core Identity
    public string color;           // "green", "red", "blue", "yellow", "none"
    public string type;            // "robot", "box", "1x2_box"
    public Vector3Int position;    // Current grid position
    public Direction rotation;     // Horizontal direction it is facing
    public bool alive;             // Whether object is currently alive
    
    // Prefab reference for respawning
    public GameObject prefab;      // Reference to the original GameObject/prefab
    
    // Constructor
    public Object(string objectColor, string objectType, Vector3Int startPos, Direction startRotation = Direction.Forward, GameObject objectPrefab = null)
    {
        color = objectColor;
        type = objectType;
        position = startPos;
        rotation = startRotation;
        alive = true;
        prefab = objectPrefab;
    }
    
    // Utility Methods
    public bool IsType(string checkType) => type == checkType;
    public bool IsColor(string checkColor) => color == checkColor;
    public bool IsAt(Vector3Int pos) => position == pos;
    public bool IsAlive() => alive;
    
    public override string ToString() => $"{color} {type} at {position} facing {rotation}";
}

/// <summary>
/// A task is something a color group of objects does in one tick
/// </summary>
[System.Serializable]
public class Task
{
    public string color;         // The colored objects performing the action
    public TaskAction action;    // The action being performed
    public Direction direction;  // The direction of movement (or None)
    
    // Constructor
    public Task(string taskColor, TaskAction taskAction, Direction taskDirection = Direction.None)
    {
        color = taskColor;
        action = taskAction;
        direction = taskDirection;
    }
    
    public override string ToString() => $"{color}, {action}, {direction}";
}

/// <summary>
/// Data for one tick of physics processing
/// </summary>
[System.Serializable]
public class TickData
{
    public int tickNumber;                    // Which tick in the step
    public List<Task> tasks;                  // All tasks happening this tick
    public List<ObjectMovement> movements;    // Individual object movements
    
    public TickData(int tick)
    {
        tickNumber = tick;
        tasks = new List<Task>();
        movements = new List<ObjectMovement>();
    }
    
    public override string ToString()
    {
        return $"Tick {tickNumber}: {tasks.Count} tasks, {movements.Count} movements";
    }
}

/// <summary>
/// Individual object movement data for animation
/// </summary>
[System.Serializable]
public class ObjectMovement
{
    public Object obj;                // The object being moved
    public Vector3Int fromPosition;   // Starting position
    public Vector3Int toPosition;     // Ending position
    public TaskAction movementType;   // Type of movement
    
    public ObjectMovement(Object gameObject, Vector3Int from, Vector3Int to, TaskAction type)
    {
        obj = gameObject;
        fromPosition = from;
        toPosition = to;
        movementType = type;
    }
    
    public override string ToString()
    {
        return $"{obj.color} {obj.type}: {fromPosition} -> {toPosition} ({movementType})";
    }
}
