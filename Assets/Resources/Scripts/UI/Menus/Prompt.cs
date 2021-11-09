using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Prompt : SubMenu
{
    public void ShowMessage(string messageText)
    {
        content.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = messageText;
        base.Enter();
    }
}
