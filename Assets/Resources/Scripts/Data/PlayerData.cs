using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Geospatial;
using System;

public class PlayerData
{
    public long PlayerDataId;
    public int Value;
    public int IncomePerSecond;
    public int ValueUpdated;
    public DateTime LastUpdate;

    public void Init()
    {
        LastUpdate = DateTime.Now;
        ValueUpdated = Value;
    }

    public void Update()
    {
        if (LastUpdate != null)
        {
            ValueUpdated +=IncomePerSecond;
        }
        else
        {
            Init();
        }
    }
}
