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
        if (password.Equals(passwordRepeated) || !ud.IsComplete())
        {
            //failed register message here
            return;
        }

        //Get token
        API api = new API();
        String connectionToken = api.RegisterAndGetToken(ud, password);

        //Finish
        if (String.IsNullOrWhiteSpace(connectionToken))
        {
            //failed login message here
        }
        else
        {
            GameObject.Find("StartupManager").GetComponent<StartupManager>().RunAsClient();
        }
    }

    public void Button_Return()
    {
        ReturnButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
