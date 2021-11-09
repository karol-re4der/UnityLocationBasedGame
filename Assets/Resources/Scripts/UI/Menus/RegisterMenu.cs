using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class RegisterMenu : SubMenu
{
    public SubMenu ReturnButtonTarget;

    public void Button_Register()
    {
        //Load login form data
        UserData ud = new UserData();
        ud.Name = content.transform.Find("Name Field").GetComponent<TMP_InputField>().text;
        ud.Surname = content.transform.Find("Surname Field").GetComponent<TMP_InputField>().text;
        ud.Nickname = content.transform.Find("Nick Field").GetComponent<TMP_InputField>().text;
        ud.Email = content.transform.Find("Nick Field").GetComponent<TMP_InputField>().text;

        String password = content.transform.Find("Password Field").GetComponent<TMP_InputField>().text;
        String passwordRepeated = content.transform.Find("Repeat Password Field").GetComponent<TMP_InputField>().text;

        //Validate login data
        if (!password.Equals(passwordRepeated) || !ud.IsComplete())
        {
            //failed register message here
            return;
        }

        ud.Password = password;

        //Get token
        Globals.IsHost = false;
        Globals.GetNetworkManager().StartNetworking();
        Globals.GetNetworkManager().SendMessageToServer("REGISTER", JsonUtility.ToJson(ud));
    }

    public void Button_Return()
    {
        ReturnButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
