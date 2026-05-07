using System.Collections.Generic;
using UnityEngine;

public class DungeonPlacementContext
{
    struct Reservation
    {
        public Vector2 Center;
        public float Radius;
    }

    readonly List<Reservation> reservations = new List<Reservation>();

    public void ReserveCircle(Vector3 worldPosition, float radius)
    {
        reservations.Add(new Reservation
        {
            Center = new Vector2(worldPosition.x, worldPosition.z),
            Radius = Mathf.Max(0f, radius)
        });
    }

    public bool CanPlaceCircle(Vector3 worldPosition, float radius)
    {
        Vector2 center = new Vector2(worldPosition.x, worldPosition.z);
        float safeRadius = Mathf.Max(0f, radius);

        for (int i = 0; i < reservations.Count; i++)
        {
            float min = safeRadius + reservations[i].Radius;
            if ((center - reservations[i].Center).sqrMagnitude < min * min)
            {
                return false;
            }
        }

        return true;
    }
}
