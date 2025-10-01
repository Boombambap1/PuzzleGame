using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoState
{
    private Dictionary<Vector3Int, GeoType> geoMap;

    public GeoState()
    {
        geoMap = new Dictionary<Vector3Int, GeoType>();
    }

    public void PlaceGeoAt(Vector3Int pos, GeoType type)
    {
        geoMap[pos] = type;
    }

    public void RemoveGeoAt(Vector3Int pos)
    {
        geoMap.Remove(pos);
    }

    public void ClearAllGeo()
    {
        geoMap.Clear();
    }

    public bool CheckVoidAt(Vector3Int pos)
    {
        return !geoMap.ContainsKey(pos);
    }

    public GeoType GetGeoTypeAt(Vector3Int pos)
    {
        return geoMap.ContainsKey(pos) ? geoMap[pos] : GeoType.None;
    }

    public bool CheckGeoTypeAt(Vector3Int pos, GeoType type)
    {
        return geoMap.TryGetValue(pos, out GeoType existingType) && existingType == type;
    }
}


