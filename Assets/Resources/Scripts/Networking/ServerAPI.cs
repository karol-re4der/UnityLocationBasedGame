using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Dynamic;
using System;
using System.Linq;
using Newtonsoft.Json;

public static class ServerAPI
{
    public static string Prepare_UPD(SpotData[] spots, NonPlayerData[] nonPlayers, PlayerData pd)
    {
        dynamic obj = new ExpandoObject();

        obj.spots = spots;
        obj.nonPlayers = nonPlayers;
        obj.pd = pd;
        string message = JsonConvert.SerializeObject(obj);

        return message;
    }

    public static string Prepare_WHOAMI(UserData ud)
    {
        dynamic obj = new ExpandoObject();
        obj.ud = ud;
        string message = JsonConvert.SerializeObject(obj);

        return message;
    }
}
