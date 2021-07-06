using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;

public class MapScaler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ResizeToFitCamera();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResizeToFitCamera()
    {
        float baseSize = Camera.main.orthographicSize * 2;
        float ratio =1f/((float)Screen.height / Screen.width);
        float shortSide = baseSize * ratio;
        gameObject.GetComponent<MapRenderer>().LocalMapDimension = new Vector2(shortSide, baseSize);

        Camera.main.orthographicSize = 0.5f;
    }
}
