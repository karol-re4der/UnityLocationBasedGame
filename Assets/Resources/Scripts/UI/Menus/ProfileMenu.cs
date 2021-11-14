using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProfileMenu : SubMenu
{
    public void Enter()
    {
        UserData ud = Globals.GetClientLogic().LatestUserData;
        if (ud!=null)
        {
            content.transform.Find("Fields/Name Field/Right Text/").GetComponent<TextMeshProUGUI>().text = ud.Name;
            content.transform.Find("Fields/Surname Field/Right Text/").GetComponent<TextMeshProUGUI>().text = ud.Surname;
            content.transform.Find("Fields/Nickname Field/Right Text/").GetComponent<TextMeshProUGUI>().text = ud.Nickname;
            content.transform.Find("Fields/Email Field/Right Text/").GetComponent<TextMeshProUGUI>().text = ud.Email;

            base.Enter();
        }
        else
        {
            string message = ClientAPI.Prepare_WHOAMI(PlayerPrefs.GetString("Token", ""));
            Globals.GetNetworkManager().SendMessageToServer("WHOAMI", message);
            Globals.GetPrompt().ShowMessage("No user data loaded yet");
        }
    }
}