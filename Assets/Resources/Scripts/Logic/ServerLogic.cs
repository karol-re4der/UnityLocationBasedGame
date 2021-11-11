using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerLogic : MonoBehaviour
{
    void Start()
    {
        if (!Globals.GetNetworkManager().IsHost)
        {
            enabled = false;
        }
    }

    void Update()
    {
        
    }
}
