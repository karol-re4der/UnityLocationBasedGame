using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    public List<GameObject> GameplayGameObjects;
    public List<MonoBehaviour> GameplayComponents;

    public List<GameObject> StartupGameObjects;
    public List<MonoBehaviour> StartupComponents;

    public void EnterGameView()
    {
        //Close startup view elements
        foreach (GameObject obj in StartupGameObjects)
        {
            obj.SetActive(false);
        }
        foreach (MonoBehaviour com in StartupComponents)
        {
            com.enabled = false;
        }

        //Open game view elements
        foreach (GameObject obj in GameplayGameObjects)
        {
            obj.SetActive(true);
        }
        foreach (MonoBehaviour com in GameplayComponents)
        {
            com.enabled = true;
        }
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

        //Hide game view elements
        foreach (GameObject obj in GameplayGameObjects)
        {
            obj.SetActive(false);
        }
        foreach (MonoBehaviour com in GameplayComponents)
        {
            com.enabled = false;
        }

        //Open startup view elements
        foreach (GameObject obj in StartupGameObjects)
        {
            obj.SetActive(true);
        }
        foreach (MonoBehaviour com in StartupComponents)
        {
            com.enabled = true;
        }
    }
}
