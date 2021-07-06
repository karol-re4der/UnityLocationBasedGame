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
        LogMessage("Running in debug mode");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LogMessage(string message)
    {
        transform.Find("Debug Text").GetComponent<TextMeshProUGUI>().text = message + "\n" + transform.Find("Debug Text").GetComponent<TextMeshProUGUI>().text;
        Debug.Log(message);
    }
}
