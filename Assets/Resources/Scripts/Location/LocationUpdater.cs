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
    public bool SendInitialUPD = true;

    private DateTime timestamp;

    void Start()
    {
        timestamp = DateTime.Now;   
    }

    void Update()
    {
        if ((DateTime.Now - timestamp).Seconds > UpdateInterval)
        {
            UpdateNow();
        }
    }

    public void UpdateNow()
    {
        Vector2 locationAsVector = Provider.GetLocation();
        LatLon location = new LatLon(locationAsVector.x, locationAsVector.y);

        if (SendInitialUPD)
        {
            if(Map.Center==new LatLon(0, 0))
            {
                if (locationAsVector != Vector2.zero)
                {
                    if (!Globals.GetNetworkManager().IsHost)
                    {
                        Map.Center = location;
                        Map.transform.Find("Player Pin").GetComponent<MapPin>().Location = location;

                        string message = ClientAPI.Prepare_WHOAMI(PlayerPrefs.GetString("Token", ""));
                        Globals.GetNetworkManager().SendMessageToServer("WHOAMI", message);

                        Invoke("InitialUPD", 1);
                    }
                    SendInitialUPD = false;
                }
            }
            else
            {
                SendInitialUPD = false;
            }
        }

        Map.Center = location;
        Map.transform.Find("Player Pin").GetComponent<MapPin>().Location = location;

        timestamp = DateTime.Now;
    }

    private void InitialUPD()
    {
        if (Map.gameObject.activeSelf)
        {
            string message = ClientAPI.Prepare_UPD(Map.Bounds, PlayerPrefs.GetString("Token", ""));
            Globals.GetNetworkManager().SendMessageToServer("UPD", message);
        }
    }
}
