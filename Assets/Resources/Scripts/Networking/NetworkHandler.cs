using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class NetworkHandler : NetworkManager
{
    void Awake()
    {
        if (Globals.IsHost)
        {
            Globals.GetNetworkManager().StartServer();
        }
        else
        {
            Globals.GetNetworkManager().StartClient();
        }
    }

    //Client side
    public override void OnStartClient()
    {
        Globals.GetDebugConsole().LogMessage("Client started");
    }

    public override void OnStopClient()
    {
        Globals.GetDebugConsole().LogMessage("Client stopped");
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Globals.GetDebugConsole().LogMessage("Connected to server");
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Globals.GetDebugConsole().LogMessage("Disconnected from server");
    }

    public override void OnClientError(Exception exception)
    {
        Globals.GetDebugConsole().LogMessage("Connection error: "+exception.Message);
    }

    //Server side
    public override void OnStartServer()
    {
        Globals.GetDebugConsole().LogMessage("Server started");
    }

    public override void OnStopServer()
    {
        Globals.GetDebugConsole().LogMessage("Server stopped");
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        Globals.GetDebugConsole().LogMessage("Client "+conn.connectionId+" connected from ip "+conn.address);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Globals.GetDebugConsole().LogMessage("Client " + conn.connectionId + " disconnected");
    }

    public override void OnServerError(NetworkConnection conn, Exception exception)
    {
        Globals.GetDebugConsole().LogMessage("Server error " + exception.Message);
    }
}
