using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using kcp2k;
using System;

public class NetworkHandler : NetworkManager
{
    public struct MessagePacket: NetworkMessage
    {
        public string content;
    }

    public void StartNetworking()
    {
        gameObject.GetComponent<KcpTransport>().Port = Globals.NetworkingPort;
        if (Globals.IsHost)
        {
            StartServer();
            SetupServerCallbacks();
        }
        else
        {
            networkAddress = Globals.ServerAddress;
            StartClient();
            SetupClientCallbacks();
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

    public void ServerMessage(string text)
    {
        foreach (NetworkConnectionToClient target in NetworkServer.connections.Values) {
            MessagePacket msg = new MessagePacket
            {
                content = text
            };
            target.Send(msg);
        }
    }

    public void SetupServerCallbacks()
    {
        
    }

    public void HandleClientMessage(MessagePacket msg)
    {
        
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

    public void SetupClientCallbacks()
    {
        NetworkClient.RegisterHandler<MessagePacket>(HandleServerMessage);
    }

    public void HandleServerMessage(MessagePacket msg)
    {
        Globals.GetDebugConsole().LogMessage("Message received: "+msg.content);
    }
}
