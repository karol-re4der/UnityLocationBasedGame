using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using TMPro;
using System.Linq;
using Mirror;
using kcp2k;


public class ClientLogic : MonoBehaviour
{
    private DateTime nextTick;
    private DateTime nextInterfaceUpdate;

    public TextMeshProUGUI ValueText;
    public PlayerData LatestPlayerData;
    public UserData LatestUserData;

    public void Clean()
    {
        LatestUserData = null;
        LatestPlayerData = null;
        ValueText.text = "";
        Globals.GetSpotMenu().CurrentSpot = null;
    }

    public void Init()
    {
        nextTick = DateTime.Now;

        Invoke("InitialUpdate", 1);
    }

    private void InitialUpdate()
    {
        if (!Globals.GetNetworkManager().IsHost && Globals.GetMap()?.activeSelf == true && !Globals.GetLoader().IsOn())
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
            return;
        }

        if (DateTime.Now > nextTick)
        {
            if (Globals.GetMap()?.activeSelf == true && !Globals.GetLoader().IsOn())
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
        if (DateTime.Now > nextInterfaceUpdate)
        {
            if (LatestPlayerData != null)
            {
                LatestPlayerData.Update();
                string text = $"{LatestPlayerData.ValueUpdated}{Globals.ValueChar} {((LatestPlayerData.IncomePerSecond < 0) ? '-' : '+')} {LatestPlayerData.IncomePerSecond} {Globals.ValueChar}/s";
                ValueText.text = text;
            }

            nextInterfaceUpdate = DateTime.Now.AddSeconds(1);
        }
    }

    public void Handle_REGISTER(bool result, string message)
    {
        if (result)
        {
            PlayerPrefs.SetString("Token", message);
            Globals.GetDebugConsole().LogMessage("REGISTER successful. New session token: " + message);
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Register Menu").GetComponent<RegisterMenu>().Exit();
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Startup Menu").GetComponent<StartupMenu>().Enter();
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetPrompt().ShowMessage(message);
            Globals.GetDebugConsole().LogMessage("REGISTER failed: " + message);
        }
    }
    public void Handle_AUTH(bool result, string message)
    {
        if (result)
        {
            PlayerPrefs.SetString("Token", message);
            Globals.GetDebugConsole().LogMessage("AUTH successful!");
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Login Menu").GetComponent<LoginMenu>().Exit();
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Startup Menu").GetComponent<StartupMenu>().Enter();
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetPrompt().ShowMessage(message);
            Globals.GetDebugConsole().LogMessage("AUTH failed: " + message);
        }
    }
    public void Handle_CHECK(bool result, string message)
    {
        if (result)
        {
            Globals.GetDebugConsole().LogMessage("CHECK successful!");
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetDebugConsole().LogMessage("CHECK failed: " + message);

            if (Globals.GetMap().activeSelf)
            {
                Globals.GetStartupManager().ExitGameView();
                Globals.GetPrompt().ShowMessage("Disconnected! " + message);
            }
        }
        Globals.GetLoader().Exit();
    }
    public void Handle_UPD(List<SpotData> spots, List<NonPlayerData> nonPlayers, PlayerData pd)
    {
        LatestPlayerData = pd;
        LatestPlayerData.Init();

        //Delete out of range spots
        List<SpotPin> existingSpots = Globals.GetMap().GetComponentsInChildren<SpotPin>().ToList<SpotPin>();
        foreach (SpotPin spot in existingSpots)
        {
            if (!spots.Exists((x) => x.Id == spot.Data.Id))
            {
                GameObject.Destroy(spot.gameObject);
            }
        }

        //Instantiate new in range spots
        foreach (SpotData spot in spots)
        {
            if (!existingSpots.Exists((x) => x.Data.Id == spot.Id))
            {
                GameObject newPin = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Spot Pin"), Globals.GetMap().transform);
                newPin.GetComponent<SpotPin>().Init(spot);
            }
        }

        //Delete outdated/outranged players 
        List<NonPlayerPin> existingNonPlayers = Globals.GetMap().GetComponentsInChildren<NonPlayerPin>().ToList<NonPlayerPin>();
        foreach (NonPlayerPin np in existingNonPlayers)
        {
            if (!nonPlayers.Exists((x) => x.UserId == np.Data.UserId))
            {
                GameObject.Destroy(np.gameObject);
            }
        }

        //Instantiate new players in range OR update the position
        foreach (NonPlayerData nonPlayer in nonPlayers)
        {
            if (!existingNonPlayers.Exists((x) => x.Data.UserId == nonPlayer.UserId))
            {
                GameObject newNonPlayer = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player Pin"), Globals.GetMap().transform);
                newNonPlayer.GetComponent<NonPlayerPin>().Init(nonPlayer);
            }
            else if (existingNonPlayers.Exists((x) => x.Data.UserId == nonPlayer.UserId))
            {
                existingNonPlayers.Find((x) => x.Data.UserId == nonPlayer.UserId).Init(nonPlayer);
            }
        }
    }
    public void Handle_KILL(string reason)
    {
        Globals.GetPrompt().ShowMessage("Disconnected by server. " + reason);

        NetworkClient.Disconnect();
        if (Globals.GetMap().activeSelf)
        {
            Globals.GetStartupManager().ExitGameView();
        }
    }
    public void Handle_BUY(bool result, long spotId, string message)
    {
        if (result)
        {
            Globals.GetPrompt().ShowMessage("Purchase successful!");
            Globals.GetDebugConsole().LogMessage("BUY successful!");

            SpotPin spot = Globals.GetMap().GetComponentsInChildren<SpotPin>().ToList<SpotPin>().Find((x) => x.Data.Id == spotId);
            spot.Data.OwnerId = spotId;
            if (Globals.GetClientLogic().LatestUserData != null)
            {
                spot.Data.OwnerNickname = Globals.GetClientLogic().LatestUserData.Nickname;
            }
            if (Globals.GetClientLogic().LatestPlayerData.Value != null)
            {
                Globals.GetClientLogic().LatestPlayerData.IncomePerSecond += spot.Data.IncomePerSecond;
                Globals.GetClientLogic().LatestPlayerData.ValueUpdated -= spot.Data.Value;
            }
            if (Globals.GetSpotMenu().IsOn())
            {
                Globals.GetSpotMenu().Enter(spot.Data);
            }
        }
        else
        {
            Globals.GetPrompt().ShowMessage("Purchase failed! " + message);
            Globals.GetDebugConsole().LogMessage("BUY failed: " + message);
        }
    }
    public void Handle_WHOAMI(UserData ud)
    {
        LatestUserData = ud;
    }
}
