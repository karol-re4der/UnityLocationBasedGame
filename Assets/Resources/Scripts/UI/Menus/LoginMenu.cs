using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class LoginMenu : SubMenu
{
    public SubMenu ReturnButtonTarget;

    public void Button_Login()
    {
        //Temporary
        GameObject.Find("StartupManager").GetComponent<StartupManager>().RunAsClient();
        return;
        //end of temporary


        //Load login form data
        String login = content.transform.Find("Login Field").GetComponent<TMP_InputField>().text;
        String password = content.transform.Find("Password Field").GetComponent<TMP_InputField>().text;

        //Get token
        API api = new API();
        String connectionToken = api.GetConnectionToken(login, password);

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
