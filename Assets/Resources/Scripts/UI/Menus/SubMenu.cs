using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubMenu : MonoBehaviour
{
    private float target;
    private Transform fade;
    private GameObject content;
    private bool raycastBlock;

    public bool startsActive = false;

    public bool IsOn()
    {
        return content.activeSelf;
    }

    void Start()
    {
        fade = transform.Find("Fade");
        content = transform.Find("Content").gameObject;
        target = fade.GetComponent<Image>().color.a;
        raycastBlock = fade.GetComponent<Image>().raycastTarget;


        if (!startsActive)
        {
            content.SetActive(false);
            fade.GetComponent<Image>().color = Color.clear;
            fade.GetComponent<Image>().raycastTarget = false;
        }
    }

    public void Enter()
    {
        CancelInvoke();
        fade.GetComponent<Image>().raycastTarget = raycastBlock;
        content.SetActive(true);
        InvokeRepeating("FadeIn", 0, 0.01f);
    }

    private void FadeIn()
    {
        Color newColor = fade.GetComponent<Image>().color;
        newColor.a += (float)(0.01);
        fade.GetComponent<Image>().color = newColor;
        if (newColor.a >= target)
        {
            CancelInvoke();
        }
    }

    public void Exit()
    {
        CancelInvoke();
        fade.GetComponent<Image>().raycastTarget = raycastBlock;
        content.SetActive(false);
        InvokeRepeating("FadeOut", 0, 0.01f);
    }

    private void FadeOut()
    {
        Color newColor = fade.GetComponent<Image>().color;
        newColor.a -= (float)(0.01);
        fade.GetComponent<Image>().color = newColor;
        if (newColor.a <= 0)
        {
            fade.GetComponent<Image>().raycastTarget = false;
            CancelInvoke();
        }
    }
}
