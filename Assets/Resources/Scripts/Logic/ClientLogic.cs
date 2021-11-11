using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;

public class ClientLogic : MonoBehaviour
{
    private DateTime nextTick;

    void Start()
    {
        nextTick = DateTime.Now;
        if (Globals.GetNetworkManager().IsHost)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (DateTime.Now > nextTick)
        {
            if (Globals.GetMap()?.activeSelf==true && !Globals.GetLoader().IsOn())
            {
                string message = ClientAPI.Prepare_UPD(Globals.GetMap().GetComponent<MapRenderer>().Bounds, PlayerPrefs.GetString("Token", ""));
                Globals.GetNetworkManager().SendMessageToServer("UPD", message);
                nextTick = DateTime.Now.AddSeconds(Globals.IntervalInSeconds_UPD);
            }
        }
    }
}
