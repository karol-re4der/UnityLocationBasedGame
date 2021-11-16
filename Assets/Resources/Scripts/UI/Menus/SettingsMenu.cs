using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class SettingsMenu : SubMenu
{
    public Button SoundButton;
    private bool _soundMuted;

    void Start()
    {
        _soundMuted = PlayerPrefs.GetInt("Sound", 1)==0;
        if (_soundMuted)
        {
            SoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "    Sound muted";
        }
        else
        {
            SoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "    Mute sound";
        }
    }

    public void Button_Sound()
    {
        if (_soundMuted)
        {
            _soundMuted = false;
            SoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "    Mute sound";
            PlayerPrefs.SetInt("Sound", 1);

        }
        else
        {
            _soundMuted = true;
            SoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "    Sound muted";
            PlayerPrefs.SetInt("Sound", 0);

        }
    }

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
