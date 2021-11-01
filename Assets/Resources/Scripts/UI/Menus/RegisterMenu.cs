using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class RegisterMenu : SubMenu
{
    public SubMenu ReturnButtonTarget;

    public void Button_Register()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void Button_Return()
    {
        ReturnButtonTarget.Enter();
        gameObject.GetComponent<SubMenu>().Exit();
    }
}
