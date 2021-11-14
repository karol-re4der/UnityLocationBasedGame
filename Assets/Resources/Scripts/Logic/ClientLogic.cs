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
    public UserData LatestUserData;

    public void Clean()
    {
        LatestUserData = null;
        LatestPlayerData = null;
        PlayerValueText.text = "";
        PlayerIncomeText.text = "";
    }

    public void Init()
    {
        nextTick = DateTime.Now;
        
        Invoke("InitialUpdate", 1);
    }

    private void InitialUpdate()
    {
        if (Globals.GetMap()?.activeSelf == true && !Globals.GetLoader().IsOn())
        {
            //WHOAMI
            string message = ClientAPI.Prepare_WHOAMI(PlayerPrefs.GetString("Token", ""));
            Globals.GetNetworkManager().SendMessageToServer("WHOAMI", message);

            //UPD
            Globals.GetLocationUpdater().UpdateNow();
            message = ClientAPI.Prepare_UPD(Globals.GetMap().GetComponent<MapRenderer>().Bounds, PlayerPrefs.GetString("Token", ""));
            Globals.GetNetworkManager().SendMessageToServer("UPD", message);
            nextTick = DateTime.Now.AddSeconds(Globals.IntervalInSeconds_UPD);
        }
    }

    void Update()
    {
        if (Globals.GetNetworkManager().IsHost)
        {
            enabled = false;
        }

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
