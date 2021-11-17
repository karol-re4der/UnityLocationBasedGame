using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LocationProvider : MonoBehaviour
{
    public Vector2 mockupLocation;

    public void StartProvider()
    {
        if (Application.isEditor)
        {
            Debug.Log("Location enabled!");
        }
        else
        {
            Input.location.Start();
        }
    }

    public Vector2 GetLocation()
    {
        Vector2 result = new Vector2(0, 0);
        if (Application.isEditor)
        {
            return mockupLocation;
        }
        else
        {
            if (Input.location.status == LocationServiceStatus.Failed || Input.location.status == LocationServiceStatus.Stopped || !Input.location.isEnabledByUser)
            {
                Globals.GetStartupManager().ExitGameView();
                Globals.GetPrompt().ShowMessage("Location disabled. Disconnected.");
                Globals.GetDebugConsole().LogMessage("Location disabled");
            }
            else if (Input.location.status != LocationServiceStatus.Initializing)
            {
                result = new Vector2(Input.location.lastData.latitude, Input.location.lastData.longitude);
            }
        }
        
        return result;
    }
}
