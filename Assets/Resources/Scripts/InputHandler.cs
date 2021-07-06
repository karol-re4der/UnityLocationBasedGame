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

    #region transitions
    private Vector3 targetPos;
    private float targetZoom;
    public float transitionRate = 0.01f;
    #endregion

    #region dragin and zooming
    public float zoomModifier = 1;
    private Vector3 dragPivot;
    private float preferredZoomLevel;
    #endregion

    #region double tap detection
    private DateTime lastTap = DateTime.MinValue;
    public float doubleTapWait = 0.5f;
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        preferredZoomLevel = (Globals.GetMap().GetComponent<MapRendererBase>().MaximumZoomLevel+ Globals.GetMap().GetComponent<MapRendererBase>().MinimumZoomLevel) / 2;
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
                                CenterCamera(Globals.GetMap().transform.position, preferredZoomLevel, instant:false);
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
        }
        else
        {
            //Special
            if (Input.GetKeyDown("space"))
            {
                CenterCamera(Globals.GetMap().transform.position, preferredZoomLevel, instant: false);
            }

            //Move camera
            transform.Translate(Vector3.right * Input.GetAxis("Horizontal") * 5 * Time.deltaTime + Vector3.up * Input.GetAxis("Vertical") * 5 * Time.deltaTime);

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

    public void CenterCamera(Vector3 newPos, float targetZoom, bool instant = true)
    {
        CancelInvoke();
        if (instant)
        {
            transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
            Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel = targetZoom;
        }
        else
        {
            targetPos = newPos;
            if(targetZoom > Globals.GetMap().GetComponent<MapRendererBase>().MaximumZoomLevel)
            {
                targetZoom = Globals.GetMap().GetComponent<MapRendererBase>().MinimumZoomLevel;
            }
            else if (targetZoom < Globals.GetMap().GetComponent<MapRendererBase>().MinimumZoomLevel)
            {
                targetZoom = Globals.GetMap().GetComponent<MapRendererBase>().MinimumZoomLevel;
            }
            this.targetZoom = targetZoom;
            InvokeRepeating("Transition", 0, transitionRate);
        }
    }
    private void Transition()
    {
        if (Vector2.Distance(transform.position, targetPos) >= transitionRate)
        {
            float y = transform.position.y;
            transform.position = Vector3.Lerp(transform.position, targetPos, transitionRate * 10);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
        if (Mathf.Abs(Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel - targetZoom) >= transitionRate)
        {
            Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel = Mathf.Lerp(Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel, targetZoom, transitionRate * 10);
        }

        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPos.x, targetPos.z)) < transitionRate && Mathf.Abs(Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel - targetZoom) < transitionRate)
        {
            transform.position = new Vector3(targetPos.x, transform.position.y ,targetPos.z);
            Globals.GetMap().GetComponent<MapRendererBase>().ZoomLevel = targetZoom;
            CancelInvoke();
        }

    }

    private void SavePivot(Vector2 pivot)
    {
        dragPivot = Camera.main.ScreenToWorldPoint(pivot);
    }

    private void DragTo(Vector2 touch)
    {
        CancelInvoke();
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
