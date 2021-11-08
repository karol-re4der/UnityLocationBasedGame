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
        public int messageId;
        public string content;
    }
    private string Token = "";
    private int LastMessageValue = 0;

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

    public void SetupClientCallbacks()
    {
        NetworkClient.RegisterHandler<MessagePacket>(HandleMessageFromServer);
    }

    public void HandleMessageFromServer(MessagePacket msg)
    {
        Globals.GetDebugConsole().LogMessage("Message received: " + msg.content);
    }

    public void SendMessageToServer(string content)
    {
        LastMessageValue++;
        MessagePacket msg = new MessagePacket
        {
            messageId = LastMessageValue,
            content = content
        };
        NetworkClient.Send(msg, 0);
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
 
    public void HandleMessageFromClient(NetworkConnection conn, MessagePacket msg)
    {
        Globals.GetDebugConsole().LogMessage("Message received: " + msg.content);
    }

    public void SendMessageToClient(string text)
    {
        Globals.GetDatabaseConnector().GetNextMessageId();
        foreach (NetworkConnectionToClient target in NetworkServer.connections.Values)
        {
            MessagePacket msg = new MessagePacket
            {
                messageId = Globals.GetDatabaseConnector().GetNextMessageId(),
                content = text
            };
            target.Send(msg);
        }
    }

    public void SetupServerCallbacks()
    {
        NetworkServer.RegisterHandler<MessagePacket>(HandleMessageFromClient);
    }
}
