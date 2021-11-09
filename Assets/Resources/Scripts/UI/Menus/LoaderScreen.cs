using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoaderScreen : SubMenu
{
    public void Enter(string loaderText)
    {
        content.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = loaderText;
        base.Enter();
    }
}
