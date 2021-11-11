using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;

public class GameplaySpotInstance : MapPin
{
    public GameplaySpot data;

    public void Init(GameplaySpot gsData)
    {
        data = gsData;
        Location = data.Coords;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
