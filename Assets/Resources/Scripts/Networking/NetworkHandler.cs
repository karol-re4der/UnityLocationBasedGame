﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using kcp2k;
using System;

public class NetworkHandler : NetworkManager
{
    public struct MessagePacket: NetworkMessage
    {
        public int MessageId;
        public string Type;
        public string Content;
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

    #region Client
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
        //Diagnostics
        string debugText = "Message " + msg.MessageId + " from server received. Content: " + msg.Content;
        Globals.GetDebugConsole().LogMessage(debugText);

        //Handling
        switch (msg.Type)
        {
            case "REGISTER":
                Client_REGISTER(msg);
                break;
            case "AUTH":
                Client_AUTH(msg);
                break;
            default:
                return;
        }

        //Diagnostics
        debugText = msg.Type + " message " + msg.MessageId + " from server handled.";
        Globals.GetDebugConsole().LogMessage(debugText);
    }

    private void Client_REGISTER(MessagePacket msg)
    {
        Globals.GetDebugConsole().LogMessage("REGISTER result: "+msg.Content);
    }

    private void Client_AUTH(MessagePacket msg)
    {

    }

    public void SendMessageToServer(string type, string content)
    {
        LastMessageValue++;
        MessagePacket msg = new MessagePacket
        {
            MessageId = LastMessageValue,
            Type = type,
            Content = content
        };
        NetworkClient.Send(msg, 0);
    }
    #endregion

    #region Server
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
        //Diagnostics
        string debugText = "Message " + msg.MessageId + " from " + conn.address + " received. Content: "+msg.Content;
        Globals.GetDebugConsole().LogMessage(debugText);
        Globals.GetDatabaseConnector().LogInDatabase("MSG", debugText);

        //Handling
        switch (msg.Type)
        {
            case "REGISTER":
                Server_REGISTER(conn, msg);
                break;
            case "AUTH":
                Server_AUTH(conn, msg);
                break;
            default:
                return;
        }

        //Diagnostics
        debugText = msg.Type + " message " + msg.MessageId + " from " + conn.address + " handled.";
        Globals.GetDebugConsole().LogMessage(debugText);
        Globals.GetDatabaseConnector().LogInDatabase("MSG", debugText);
    }

    private void Server_REGISTER(NetworkConnection conn, MessagePacket msg)
    {
        UserData ud = JsonUtility.FromJson<UserData>(msg.Content);

        if (ud.IsComplete())
        {
            if (ud.Password.Length > 6 && !String.IsNullOrWhiteSpace(ud.Password))
            {
                string newToken = GenerateToken();
                if (!String.IsNullOrWhiteSpace(newToken))
                {
                    if (!Globals.GetDatabaseConnector().UserExists(ud))
                    {
                        int userId = Globals.GetDatabaseConnector().InsertNewUser(ud);
                        int sessionId = Globals.GetDatabaseConnector().InsertNewSession(newToken);
                        Globals.GetDatabaseConnector().AssignSession(sessionId, userId);
                        SendMessageToClient((NetworkConnectionToClient)conn, "REGISTER", "{\"success\": true, \"msg\": \"" + newToken + "\"}");
                    }
                    else
                    {
                        SendMessageToClient((NetworkConnectionToClient)conn, "REGISTER", "{\"success\": false, \"msg\": \"Email or nickname already in use.\"}");
                    }
                }
                else
                {
                    SendMessageToClient((NetworkConnectionToClient)conn, "REGISTER", "{\"success\": false, \"msg\": \"Server-error: cannot generate new token.\"}");
                }
            }
            else
            {
                SendMessageToClient((NetworkConnectionToClient)conn, "REGISTER", "{\"success\": false, \"msg\": \"Password invalid.\"}");
            }
        }
        else
        {
            SendMessageToClient((NetworkConnectionToClient)conn, "REGISTER", "{\"success\": false, \"msg\": \"Credentials incomplete.\"}");
        }
    }

    private void Server_AUTH(NetworkConnection conn, MessagePacket msg)
    {

    }

    public void SendMessageToClient(NetworkConnectionToClient conn, string type, string text)
    {
        Globals.GetDatabaseConnector().GetNextMessageId();

        MessagePacket msg = new MessagePacket
        {
            MessageId = Globals.GetDatabaseConnector().GetNextMessageId(),
            Type = type,
            Content = text
        };

        conn.Send(msg);
    }

    public void SetupServerCallbacks()
    {
        NetworkServer.RegisterHandler<MessagePacket>(HandleMessageFromClient);
    }

    private string GenerateToken()
    {
        string newToken = "";
        int attempts = 0;

        do
        {
            if (attempts > 10)
            {
                return "";
            }

            newToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 16);
            attempts++;
        } while (Globals.GetDatabaseConnector().TokenInUse(newToken));

        return newToken;
    }
    #endregion
}
