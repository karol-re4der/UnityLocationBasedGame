using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationProvider : MonoBehaviour
{
    public Vector2 mockupLocation;

    void Start()
    {
        if (Application.isEditor)
        {
            Debug.Log("Location enabled!");
        }
        else
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location disabled by user!");
            }
            else
            {
                Input.location.Start();
            }
        }
    }

    void Update()
    {
        
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
            if (Input.location.isEnabledByUser && Input.location.status != LocationServiceStatus.Initializing)
            {
                if (Input.location.status == LocationServiceStatus.Failed)
                {
                    Debug.Log("Location Service Failed!");
                }
                else
                {
                    result = new Vector2(Input.location.lastData.latitude, Input.location.lastData.longitude);
                    //Debug.Log("Location taken: " + result.x + ":" + result.y);
                }
            }
        }
        
        return result;
    }
}
