using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using kcp2k;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;

public class NetworkHandler : NetworkManager
{
    public struct MessagePacket : NetworkMessage
    {
        public string Type;
        public string Content;
    }
    private bool _beenConnected = false;
    public bool IsHost = false;

    void Start()
    {
        gameObject.GetComponent<KcpTransport>().Port = Globals.NetworkingPort;
        if (IsHost)
        {
            StartServer();
            SetupServerCallbacks();
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            networkAddress = Globals.ServerAddress;
            StartClient();
            SetupClientCallbacks();
        }
    }

    void Update()
    {

    }

    public void TestFunc()
    {
        SendMessageToServer("KILL", PlayerPrefs.GetString("Token", ""));
    }

    #region Client
    public override void OnStartClient()
    {
        Globals.GetDebugConsole().LogMessage("Attempting to connect...");
    }

    public override void OnStopClient()
    {
        Globals.GetDebugConsole().LogMessage("Client stopped");
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        _beenConnected = true;

        string token = PlayerPrefs.GetString("Token", "");
        if (String.IsNullOrWhiteSpace(token))
        {
            Globals.GetDebugConsole().LogMessage("Connected to server - no existing session");
            Globals.GetLoader().Exit();
        }
        else
        {
            Globals.GetDebugConsole().LogMessage("Connected to server - checking existing session");

            string message = ClientAPI.Prepare_CHECK(token);
            Globals.GetNetworkManager().SendMessageToServer("CHECK", message);
        }
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Globals.GetDebugConsole().LogMessage("No server connection");
        if (_beenConnected)
        {
            Globals.GetLoader().Enter("Reconnecting");
        }
        Invoke("StartClient", 1);
    }

    public override void OnClientError(Exception exception)
    {
        Globals.GetDebugConsole().LogMessage("Connection error: " + exception.Message);
    }

    public void SetupClientCallbacks()
    {
        NetworkClient.RegisterHandler<MessagePacket>(HandleMessageFromServer);
    }

    public void HandleMessageFromServer(MessagePacket msg)
    {
        //Diagnostics
        string debugText = msg.Type+" from server received. Content: " + msg.Content;
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
            case "CHECK":
                Client_CHECK(msg);
                break;
            case "UPD":
                Client_UPD(msg);
                break;
            case "KILL":
                Client_KILL(msg);
                break;
            case "WHOAMI":
                Client_WHOAMI(msg);
                break;
            case "BUY":
                Client_BUY(msg);
                break;
            default:
                debugText = msg.Type+" is an unknown message type. Cannot handle.";
                Globals.GetDebugConsole().LogMessage(debugText);
                return;
        }

        //Diagnostics
        debugText = msg.Type + " message from server handled.";
        Globals.GetDebugConsole().LogMessage(debugText);
    }

    private void Client_REGISTER(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.msg;
        bool result = obj.success;

        Globals.GetClientLogic().Handle_REGISTER(result, message);
    }

    private void Client_AUTH(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.msg;
        bool result = obj.success;

        Globals.GetClientLogic().Handle_AUTH(result, message);
    }

    private void Client_CHECK(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.success ? "" : obj.msg;
        bool result = obj.success;

        Globals.GetClientLogic().Handle_CHECK(result, message);
    }

    private void Client_UPD(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        List<SpotData> spots = obj.spots.ToObject<List<SpotData>>();
        List<NonPlayerData> nonPlayers = obj.nonPlayers.ToObject<List<NonPlayerData>>();
        PlayerData pd = obj.pd.ToObject<PlayerData>();

        Globals.GetClientLogic().Handle_UPD(spots, nonPlayers, pd);
    }

    private void Client_KILL(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string reason = obj.reason;

        Globals.GetClientLogic().Handle_KILL(reason);
    }

    private void Client_BUY(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        bool result = obj.success;
        long spotId = result ? obj.msg : -1;
        string message = result ? "" : obj.msg;

        Globals.GetClientLogic().Handle_BUY(result, spotId, message);
    }

    private void Client_WHOAMI(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        UserData ud = obj.ud.ToObject<UserData>();

        Globals.GetClientLogic().Handle_WHOAMI(ud);
    }

    public void SendMessageToServer(string type, string content)
    {
        MessagePacket msg = new MessagePacket
        {
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
        Globals.GetLoader().Exit();
    }

    public override void OnStopServer()
    {
        Globals.GetDebugConsole().LogMessage("Server stopped");
        Globals.GetDatabaseConnector().LogInDatabase("ServerStop", "Server was stopped");
        Globals.GetLoader().Enter("Restarting");
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
        string debugText = msg.Type + " message from " + conn.address + " received. Content: " + msg.Content;
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
            case "CHECK":
                Server_CHECK(conn, msg);
                break;
            case "UPD":
                Server_UPD(conn, msg);
                break;
            case "WHOAMI":
                Server_WHOAMI(conn, msg);
                break;
            case "BUY":
                Server_BUY(conn, msg);
                break;
            case "KILL":
                Server_KILL(conn, msg);
                break;
            default:
                debugText = msg.Type + " from " + conn.address + " is of an unknown type. Cannot handle.";
                Globals.GetDebugConsole().LogMessage(debugText);
                Globals.GetDatabaseConnector().LogInDatabase("MSG", debugText);
                return;
        }

        //Diagnostics
        debugText = msg.Type + " message from " + conn.address + " handled.";
        Globals.GetDebugConsole().LogMessage(debugText);
        Globals.GetDatabaseConnector().LogInDatabase("MSG", debugText);
    }

    private void Server_REGISTER(NetworkConnection conn, MessagePacket msg)
    {
        UserData ud = JsonUtility.FromJson<UserData>(msg.Content);

        Globals.GetServerLogic().Handle_REGISTER((NetworkConnectionToClient)conn, ud);
    }

    private void Server_AUTH(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string login = obj.login;
        string pass = obj.pass;

        Globals.GetServerLogic().Handle_AUTH((NetworkConnectionToClient)conn, login, pass);
    }

    private void Server_CHECK(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;

        Globals.GetServerLogic().Handle_CHECK((NetworkConnectionToClient)conn, token);
    }

    private void Server_UPD(NetworkConnection conn, MessagePacket msg)
    {
        dynamic msgObj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        LatLon bottomLeft = new LatLon(msgObj.p1lat, msgObj.p1lon);
        LatLon topRight = new LatLon(msgObj.p2lat, msgObj.p2lon);
        string token = msgObj.token;
        GeoBoundingBoxBuilder growBox = new GeoBoundingBoxBuilder();
        growBox.Grow(bottomLeft);
        growBox.Grow(topRight);
        GeoBoundingBox bounds = growBox.ToGeoBoundingBox();

        Globals.GetServerLogic().Handle_UPD((NetworkConnectionToClient)conn, token, bounds);
    }

    private void Server_WHOAMI(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;

        Globals.GetServerLogic().Handle_WHOAMI((NetworkConnectionToClient) conn, token);
    }

    private void Server_BUY(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;
        long spotId = obj.spotId;

        Globals.GetServerLogic().Handle_BUY((NetworkConnectionToClient)conn, token, spotId);
    }

    private void Server_KILL(NetworkConnection conn, MessagePacket msg)
    {
        KillClient((NetworkConnectionToClient)conn, "Wrong request", msg.Content);
    }

    public void SendMessageToClient(NetworkConnectionToClient conn, string type, string text)
    {
        MessagePacket msg = new MessagePacket
        {
            Type = type,
            Content = text
        };

        conn.Send(msg);
        Globals.GetDatabaseConnector().LogInDatabase("MSG", $"{type} message sent to {conn.address}, content: {text}");
    }

    public void SetupServerCallbacks()
    {
        NetworkServer.RegisterHandler<MessagePacket>(HandleMessageFromClient);
    }

    public void KillClient(NetworkConnectionToClient conn, string reason, string token = "")
    {
        SendMessageToClient((NetworkConnectionToClient)conn, "KILL", "{\"msg\": \"" + reason + "\"}");
        conn.Disconnect();

        if (!String.IsNullOrWhiteSpace(token))
        {
            Globals.GetDatabaseConnector().RemoveToken(token);
        }
    }

    #endregion
}
