using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Geospatial;

public class NonPlayerData
{
    public long UserId;
    public LatLon Coords
    {
        get
        {
            return new LatLon(Lat, Lon);
        }
    }

    public double Lat;
    public double Lon;
}
