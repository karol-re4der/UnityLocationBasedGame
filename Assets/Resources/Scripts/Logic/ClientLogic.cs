using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using TMPro;

public class ClientLogic : MonoBehaviour
{
    private DateTime nextTick;
    private DateTime nextInterfaceUpdate;

    public TextMeshProUGUI PlayerValueText;
    public TextMeshProUGUI PlayerIncomeText;
    public PlayerData LatestPlayerData;

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

        RefreshInterface();
    }

    void RefreshInterface()
    {
        if(DateTime.Now> nextInterfaceUpdate)
        {
            if (LatestPlayerData != null)
            {
                LatestPlayerData.Update();
                PlayerValueText.text = "" + LatestPlayerData.ValueUpdated;
                PlayerIncomeText.text = "" + LatestPlayerData.IncomePerSecond;
            }

            nextInterfaceUpdate = DateTime.Now.AddSeconds(1);
        }
    }
}
