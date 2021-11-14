using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Geospatial;

public class SpotData
{
    public LatLon Coords
    {
        get
        {
            return new LatLon(Lat, Lon);
        }
    }

    public long Id;
    public string Name;
    public string Description;
    public int Value;
    public long OwnerId;

    public double Lat;
    public double Lon;
}
