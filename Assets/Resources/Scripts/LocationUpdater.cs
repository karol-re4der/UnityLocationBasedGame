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
            UpdateNow();
        }
    }

    public void UpdateNow()
    {
        Vector3 locationAsVector = Provider.GetLocation();
        LatLon location = new LatLon(locationAsVector.x, locationAsVector.y);
        Map.Center = location;
        Map.transform.Find("Player Pin").GetComponent<MapPin>().Location = location;
        timestamp = DateTime.Now;
    }
}
