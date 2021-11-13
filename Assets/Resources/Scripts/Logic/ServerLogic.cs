using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ServerLogic : MonoBehaviour
{
    private DateTime nextTick;
    void Start()
    {
        if (!Globals.GetNetworkManager().IsHost)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (DateTime.Now > nextTick)
        {
            DateTime timestamp = DateTime.Now;
            if (Globals.GetMap()?.activeSelf == true && !Globals.GetLoader().IsOn())
            {
                nextTick = DateTime.Now.AddSeconds(Globals.IntervalInSeconds_UPD);

                List<long> users = Globals.GetDatabaseConnector().GetUserIds();

                foreach(long id in users)
                {
                    PlayerData pd = Globals.GetDatabaseConnector().GetPlayerData(id);

                    pd.IncomePerSecond = Globals.GetDatabaseConnector().CountPlayerIncome(id);
                    pd.Value += pd.IncomePerSecond * Globals.IntervalInSeconds_UPD;

                    Globals.GetDatabaseConnector().UpdatePlayerData(pd);
                }

                Globals.GetDatabaseConnector().LogInDatabase("TICK", "Tick completed in "+(DateTime.Now-timestamp).Milliseconds+"ms");
            }
        }
    }
}
