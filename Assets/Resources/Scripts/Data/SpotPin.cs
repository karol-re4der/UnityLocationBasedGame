using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;

public class SpotPin : MapPin
{
    public SpotData Data;

    public void Init(SpotData spotData)
    {
        Data = spotData;
        Location = Data.Coords;
    }
}
