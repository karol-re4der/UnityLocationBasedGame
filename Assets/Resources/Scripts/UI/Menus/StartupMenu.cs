using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupMenu : SubMenu
{
    public SubMenu LoginButtonTarget;
    public SubMenu RegisterButtonTarget;

    public void Button_Login()
    {
        LoginButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
    public void Button_Register()
    {
        RegisterButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
