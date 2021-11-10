using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using System.Dynamic;
using Newtonsoft.Json;

public class LoginMenu : SubMenu
{
    public SubMenu ReturnButtonTarget;

    public void Button_Login()
    {
        //Load input
        String login = content.transform.Find("Login Field").GetComponent<TMP_InputField>().text;
        String password = content.transform.Find("Password Field").GetComponent<TMP_InputField>().text;

        //Validate input
        if (String.IsNullOrWhiteSpace(login) || String.IsNullOrWhiteSpace(password))
        {
            Globals.GetPrompt().ShowMessage("Fill login and password to log in!");
            return;
        }

        //Request authentication
        string message = ClientAPI.Prepare_AUTH(login, password);
        Globals.GetNetworkManager().SendMessageToServer("AUTH", message);
    }

    public void Button_Return()
    {
        ReturnButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
