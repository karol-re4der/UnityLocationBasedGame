using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SettingsMenu : SubMenu
{
    public void Button_Exit()
    {
        Application.Quit();
    }
    public void Button_Logout()
    {
        base.Exit();
        PlayerPrefs.SetString("Token", "");
        Globals.GetStartupManager().ExitGameView();
        Globals.GetDebugConsole().LogMessage("Logged off!");
        Globals.GetPrompt().ShowMessage("Logged off!");
    }
}
