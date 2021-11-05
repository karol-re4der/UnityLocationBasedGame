using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    public static bool IsHost = false;

    public static GameObject GetMap()
    {
        return GameObject.Find("Gameplay Space/Map");
    }

    public static DebugMode GetDebugConsole()
    {
        return GameObject.Find("Canvas/DebugUI").GetComponent<DebugMode>();
    }

    public static InputHandler GetInput()
    {
        return Camera.main.GetComponent<InputHandler>();
    }
}
