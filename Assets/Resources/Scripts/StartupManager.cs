using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    public void RunAsClient()
    {
        Globals.IsHost = false;
        SceneManager.LoadScene("GameScene");
    }

    public void RunAsHost()
    {
        Globals.IsHost = true;
        SceneManager.LoadScene("GameScene");
    }
}
