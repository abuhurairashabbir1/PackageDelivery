using UnityEngine;

/// <summary>
/// Shared information about the 3-story building (floor heights and names).
/// </summary>
public static class BuildingFloors
{
    public const int FloorCount = 3;
    public const float FloorHeight = 4f;

    /// <summary>Height (Y) of the walkable surface of a floor.</summary>
    public static float GetFloorSurfaceHeight(int floorIndex)
    {
        return floorIndex * FloorHeight;
    }

    /// <summary>Returns the floor index (0 = ground) for a world Y position.</summary>
    public static int GetFloorIndex(float worldY)
    {
        int floorIndex = Mathf.FloorToInt((worldY + 1f) / FloorHeight);
        return Mathf.Clamp(floorIndex, 0, FloorCount - 1);
    }

    public static string GetFloorName(int floorIndex)
    {
        switch (floorIndex)
        {
            case 0: return "Ground Floor";
            case 1: return "1st Floor";
            case 2: return "2nd Floor";
            default: return "Floor " + floorIndex;
        }
    }
}
