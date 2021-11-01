using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class LoginMenu : SubMenu
{
    public SubMenu ReturnButtonTarget;

    public void Button_Login()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void Button_Return()
    {
        ReturnButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
