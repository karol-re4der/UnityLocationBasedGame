using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using Microsoft.Geospatial;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public MapRenderer Map;
    public float zoomModifier = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.isEditor)
        {
            if (!EventSystem.current.IsPointerOverGameObject(0))
            {
                if (Input.touches.Length > 0)
                {
                    if (Input.touches.Length == 2)
                    {
                        //if (Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began)
                        //{
                        //    SavePivot(Vector2.Lerp(Input.touches[0].position, Input.touches[1].position, 0.5f));
                        //}
                        //else if (Input.touches[0].phase == TouchPhase.Moved || Input.touches[1].phase == TouchPhase.Moved)
                        //{
                        //    DragTo(Vector2.Lerp(Input.touches[0].position, Input.touches[1].position, 0.5f));
                        //}

                        if (Input.touches[0].deltaPosition != Vector2.zero || Input.touches[1].deltaPosition != Vector2.zero)
                        {
                            float a = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
                            float b = Vector2.Distance(Input.touches[0].position + Input.touches[0].deltaPosition, Input.touches[1].position + Input.touches[1].deltaPosition);
                            float delta = a / b;
                            delta = ((delta-1)*zoomModifier)+1;
                            GameObject.Find("Canvas/DebugUI").GetComponent<DebugMode>().LogMessage("ABC: "+delta);
                            Map.ZoomLevel /= delta;
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                Map.ZoomLevel *= 1.05f;
            }
            else if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Map.ZoomLevel *= 0.95f;
            }
        }
    }
}
