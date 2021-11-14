using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class StartupManager : MonoBehaviour
{
    public void EnterGameView()
    {
        //Clear text fields
        foreach(TMP_InputField field in GameObject.Find("Canvas").transform.Find("Startup View/Menus/Login Menu/Content").gameObject.GetComponentsInChildren<TMP_InputField>())
        {
            field.text = "";
        }
        foreach (TMP_InputField field in GameObject.Find("Canvas").transform.Find("Startup View/Menus/Register Menu/Content").gameObject.GetComponentsInChildren<TMP_InputField>())
        {
            field.text = "";
        }

        //Close startup view elements
        GameObject.Find("Canvas").transform.Find("Startup View").gameObject.SetActive(false);

        //Open game view elements
        GameObject.Find("Canvas").transform.Find("Game View").gameObject.SetActive(true);
        Globals.GetMap().SetActive(true);
        Globals.GetInput().enabled = true;

        //Start logic
        Globals.GetClientLogic().Init();
    }

    public void ExitGameView()
    {
        //Clear pins
        foreach (Transform trans in Globals.GetMap().transform)
        {
            if (trans.GetComponent<SpotPin>())
            {
                GameObject.Destroy(trans.gameObject);
            }
            if (trans.GetComponent<NonPlayerPin>())
            {
                GameObject.Destroy(trans.gameObject);
            }
        }

        //Close game view elements
        GameObject.Find("Canvas").transform.Find("Startup View").gameObject.SetActive(false);
        Globals.GetMap().SetActive(false);
        Globals.GetInput().enabled = false;

        //Open startup view elements
        GameObject.Find("Canvas").transform.Find("Startup View").gameObject.SetActive(true);

        //Stop logic
        Globals.GetClientLogic().Clean();
    }
}
