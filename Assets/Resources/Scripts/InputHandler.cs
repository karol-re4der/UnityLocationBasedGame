using System;
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
    public Vector3 shift;

    private DateTime lastTap = DateTime.MinValue;
    public float doubleTapWait = 0.5f;

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

                            if (DateTime.Now - lastTap < TimeSpan.FromSeconds(doubleTapWait))
                            {
                                CenterCamera();
                            }
                            lastTap = DateTime.Now;
                        }
                        else if (Input.touches[0].phase == TouchPhase.Moved)
                        {
                            DragTo(Input.touches[0].position);
                        }
                    }
                }
                FixBounds();
            }

            shift = Globals.GetMap().transform.position - Camera.main.transform.position;
        }
        else
        {
            //Move camera
            transform.Translate(Vector3.right * Input.GetAxis("Horizontal") * 5 * Time.deltaTime + Vector3.up * Input.GetAxis("Vertical") * 5 * Time.deltaTime);
            shift = Globals.GetMap().transform.position - Camera.main.transform.position;

            //Zoom camera
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                Map.ZoomLevel *= 1.05f;
            }
            else if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Map.ZoomLevel *= 0.95f;
            }

            FixBounds();
        }
    }

    public void CenterCamera()
    {
        Globals.GetDebugConsole().LogMessage("Pos centered!");
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
        Globals.GetDebugConsole().LogMessage("Pos: " + transform.position);
    }

    private void FixBounds()
    {
        float xMaxBound = Globals.GetMap().transform.position.x + Globals.GetMap().GetComponent<Collider>().bounds.extents.x / 2;
        float xMinBound = Globals.GetMap().transform.position.x - Globals.GetMap().GetComponent<Collider>().bounds.extents.x / 2;
        float zMaxBound = Globals.GetMap().transform.position.z + Globals.GetMap().GetComponent<Collider>().bounds.extents.z / 2;
        float zMinBound = Globals.GetMap().transform.position.z - Globals.GetMap().GetComponent<Collider>().bounds.extents.z / 2;

        if (transform.position.x < xMinBound)
        {
            transform.position = new Vector3(xMinBound, transform.position.y, transform.position.z);
        }
        else if (transform.position.x > xMaxBound)
        {
            transform.position = new Vector3(xMaxBound, transform.position.y, transform.position.z);
        }
        if (transform.position.z < zMinBound)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, zMinBound);
        }
        else if (transform.position.z > zMaxBound)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, zMaxBound);
        }
    }
}
