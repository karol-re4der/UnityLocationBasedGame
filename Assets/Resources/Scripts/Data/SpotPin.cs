using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using UnityEngine.EventSystems;

public class SpotPin : MapPin
{
    public SpotData Data;
    public SpotMenu TargetMenu;

    public void Init(SpotData spotData)
    {
        Data = spotData;
        Location = Data.Coords;
        TargetMenu = Globals.GetSpotMenu();
    }

    void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            TargetMenu.Enter(Data);
        }
    }
}
