using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// All new types live in NewArch to avoid name collisions with the old scripts.
// Once the old scripts are deleted:
//   1. Remove the `namespace NewArch { }` wrapper in this file.
//   2. Remove `using NewArch;` from every other new script.
// ─────────────────────────────────────────────────────────────────────────────

namespace NewArch
{

// ══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Types of static geometry tiles placed in GeoStateV2.</summary>
public enum GeoType
{
    None,
    Block,
    Exit,
    Spawn
}

/// <summary>The logical type of a movable game object.</summary>
public enum ObjectType
{
    Player,
    Box,
    LargeBox    // 1x2 box occupying two grid cells
}

/// <summary>The kind of movement recorded in a TickData entry.</summary>
public enum MoveType
{
    Walk,       // Player walking or carrying
    Slide,      // Object pushed sideways
    Fall,       // Gravity-driven downward move
    Die,        // Object died (fell off-world)
    Respawn     // Object placed back at its spawn point
}

// ══════════════════════════════════════════════════════════════════════════════
// GAME OBJECT
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents one movable entity in the game world (player or box).
/// Holds all logical state; visual state lives in AnimationControlV2.
/// </summary>
public class GameObj
{
    // Identity
    public string     color;
    public ObjectType type;
    public GameObject prefab;

    // Spatial
    public Vector3Int position;
    public Vector3Int rotation;

    // Physics
    public bool       alive;
    public Vector3Int movement;

    public GameObj(string color, ObjectType type, Vector3Int position, Vector3Int rotation, GameObject prefab)
    {
        this.color    = color;
        this.type     = type;
        this.position = position;
        this.rotation = rotation;
        this.prefab   = prefab;
        this.alive    = true;
        this.movement = Vector3Int.zero;
    }

    public bool IsAlive() => alive;

    public Vector3Int GetSecondaryPosition()
    {
        return type == ObjectType.LargeBox ? position + rotation : position;
    }

    public Vector3Int[] GetOccupiedPositions()
    {
        return type == ObjectType.LargeBox
            ? new[] { position, GetSecondaryPosition() }
            : new[] { position };
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// TICK / STEP DATA
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>A snapshot of one object's movement during a single tick.</summary>
public class ObjectMovement
{
    public GameObj    obj;
    public Vector3Int fromPos;
    public Vector3Int toPos;
    public MoveType   moveType;

    public ObjectMovement(GameObj obj, Vector3Int fromPos, Vector3Int toPos, MoveType moveType)
    {
        this.obj      = obj;
        this.fromPos  = fromPos;
        this.toPos    = toPos;
        this.moveType = moveType;
    }
}

/// <summary>All movements that occurred during one tick of the physics loop.</summary>
public class TickData
{
    public int                  tickNumber;
    public List<ObjectMovement> movements = new();

    public TickData(int tickNumber) { this.tickNumber = tickNumber; }
    public void AddMovement(ObjectMovement movement) => movements.Add(movement);
}

/// <summary>
/// All ticks produced during one full physics step (one player input).
/// Passed from GamePhysicsV2 to AnimationControlV2.
/// </summary>
public class StepData
{
    public List<TickData> ticks = new();
}

} // namespace NewArch
