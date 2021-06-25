using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using Microsoft.Geospatial;

public class LocationUpdater : MonoBehaviour
{
    public MapRenderer Map;
    public LocationProvider Provider;
    public float UpdateInterval;

    private DateTime timestamp;

    // Start is called before the first frame update
    void Start()
    {
        timestamp = DateTime.Now;   
    }

    // Update is called once per frame
    void Update()
    {
        if ((DateTime.Now - timestamp).Seconds > UpdateInterval)
        {
            Vector2 loc = Provider.GetLocation();
            Map.Center = new LatLon(loc.x, loc.y);
            timestamp = DateTime.Now;
        }
    }
}
