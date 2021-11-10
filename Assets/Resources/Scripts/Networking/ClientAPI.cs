using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Dynamic;

public static class ClientAPI
{
    public static string Prepare_AUTH(string login, string password)
    {
        dynamic obj = new ExpandoObject();
        obj.login = login;
        obj.pass = password;
        string message = JsonConvert.SerializeObject(obj);

        return message;
    }

    public static string Prepare_REGISTER(UserData ud)
    {
        return JsonUtility.ToJson(ud);
    }

    public static string Prepare_CHECK(string token)
    {
        dynamic obj = new ExpandoObject();
        obj.token = token;
        string message = JsonConvert.SerializeObject(obj);

        return message;
    }

    public static string Prepare_NEWPOS(Vector2 coords)
    {
        dynamic obj = new ExpandoObject();
        obj.co_lat = coords.x;
        obj.co_long = coords.y;
        string message = JsonConvert.SerializeObject(obj);

        return message;
    }
}
