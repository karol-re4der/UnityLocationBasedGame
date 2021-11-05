using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class NetworkHandler : NetworkManager
{
    void Start()
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
        Globals.GetDatabaseConnector().LogInDatabase("ServerStart", "Server was started");
    }

    public override void OnStopServer()
    {
        Globals.GetDebugConsole().LogMessage("Server stopped");
        Globals.GetDatabaseConnector().LogInDatabase("ServerStop", "Server was stopped");
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        string messageText = "Client " + conn.connectionId + " connected from ip " + conn.address;
        Globals.GetDebugConsole().LogMessage(messageText);
        Globals.GetDatabaseConnector().LogInDatabase("Traffic", messageText);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        string messageText = "Client " + conn.connectionId + " disconnected";
        Globals.GetDebugConsole().LogMessage(messageText);
        Globals.GetDatabaseConnector().LogInDatabase("Traffic", messageText);
    }

    public override void OnServerError(NetworkConnection conn, Exception exception)
    {
        Globals.GetDebugConsole().LogMessage("ServerErr" + exception.Message);
    }
}
