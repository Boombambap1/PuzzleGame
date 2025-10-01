using System.Collections.Generic;
using UnityEngine;
 public class GameState
{
    private Dictionary<Vector3Int, GameObjectData> objects;

    public GameState()
    {
        objects = new Dictionary<Vector3Int, GameObjectData>();
    }

    public void PlaceObjectAt(GameObjectData obj, Vector3Int pos)
    {
        objects[pos] = obj;
    }

    public void RemoveObjectAt(Vector3Int pos)
    {
        objects.Remove(pos);
    }

    public void MoveObjectTo(GameObjectData obj, Vector3Int newPos)
    {
        Vector3Int oldPos = obj.Position;
        objects.Remove(oldPos);
        obj.Position = newPos;
        objects[newPos] = obj;
    }

    public void ClearAllObjects()
    {
        objects.Clear();
    }

    public GameObjectData GetObjectAt(Vector3Int pos)
    {
        objects.TryGetValue(pos, out GameObjectData obj);
        return obj;
    }

    public Vector3Int GetObjectPos(GameObjectData obj)
    {
        return obj.Position;
    }

    public bool IsWinningState()
    {
        // You can fill this in later
        return false;
    }
}

public class GameObjectData
{
    public Vector3Int Position;
    public string Color;
    public bool IsAlive;
    public bool InFreefall;

    public GameObjectData(Vector3Int pos, string color)
    {
        Position = pos;
        Color = color;
        IsAlive = true;
        InFreefall = false;
    }
}


