using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using Microsoft.Geospatial;

public class NonPlayerPin : MapPin
{
    public NonPlayerData Data;

    public void Init(NonPlayerData ppdata)
    {
        this.Data = ppdata;
        Location = Data.Coords;
    }
}
