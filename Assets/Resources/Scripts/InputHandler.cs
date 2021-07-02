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
    private Vector3 dragPivot;

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
                        //Move camera
                        if (Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began)
                        {
                            SavePivot(Vector2.Lerp(Input.touches[0].position, Input.touches[1].position, 0.5f));
                        }
                        else if (Input.touches[0].phase == TouchPhase.Moved || Input.touches[1].phase == TouchPhase.Moved)
                        {
                            DragTo(Vector2.Lerp(Input.touches[0].position, Input.touches[1].position, 0.5f));
                        }

                        //Zoom camera
                        if (Input.touches[0].deltaPosition != Vector2.zero || Input.touches[1].deltaPosition != Vector2.zero)
                        {
                            float a = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
                            float b = Vector2.Distance(Input.touches[0].position + Input.touches[0].deltaPosition, Input.touches[1].position + Input.touches[1].deltaPosition);
                            float delta = a / b;
                            delta = ((delta-1)*zoomModifier)+1;
                            Map.ZoomLevel /= delta;
                        }
                    }
                    else if (Input.touches.Length == 1)
                    {
                        if (Input.touches[0].phase == TouchPhase.Began)
                        {
                            SavePivot(Input.touches[0].position);
                        }
                        else if (Input.touches[0].phase == TouchPhase.Moved)
                        {
                            DragTo(Input.touches[0].position);
                        }
                    }
                }
            }
        }
        else
        {
            //Move camera
            transform.Translate(Vector3.left * Input.GetAxis("Horizontal") * 5 * Time.deltaTime + Vector3.back * Input.GetAxis("Vertical") * 5 * Time.deltaTime);

            //Zoom camera
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

    private void SavePivot(Vector2 pivot)
    {
        dragPivot = Camera.main.ScreenToWorldPoint(pivot);
    }

    private void DragTo(Vector2 touch)
    {
        Vector3 currentPos = new Vector3(touch.x, touch.y, 0);
        currentPos = Camera.main.ScreenToWorldPoint(currentPos);
        Vector3 offset = dragPivot - currentPos;
        transform.position = transform.position + offset;
        GameObject.Find("Canvas/DebugUI").GetComponent<DebugMode>().LogMessage("Pos: " + transform.position);

    }

}
