using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugMode : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(Debug.isDebugBuild);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LogMessage(string message)
    {
        try
        {
            transform.Find("Debug Text").GetComponent<TextMeshProUGUI>().text = message + "\n" + transform.Find("Debug Text").GetComponent<TextMeshProUGUI>().text;
        }
        catch(NullReferenceException ex)
        {

        }
        Debug.Log(message);
    }
}
