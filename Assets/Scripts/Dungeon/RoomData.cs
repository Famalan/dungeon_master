using UnityEngine;
using System.Collections.Generic;

public enum RoomType
{
    Start,
    Normal,
    Exit
}

public class RoomData
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public RoomType Type;
    public int DistanceFromStart;
    public int GraphStepsFromStart = int.MaxValue;
    public List<int> ConnectedRoomIndexes = new List<int>();

    public Vector2Int Center
    {
        get { return new Vector2Int(X + Width / 2, Y + Height / 2); }
    }

    public bool Overlaps(RoomData other, int padding)
    {
        bool noOverlap = X + Width + padding <= other.X
            || other.X + other.Width + padding <= X
            || Y + Height + padding <= other.Y
            || other.Y + other.Height + padding <= Y;

        return !noOverlap;
    }
}
