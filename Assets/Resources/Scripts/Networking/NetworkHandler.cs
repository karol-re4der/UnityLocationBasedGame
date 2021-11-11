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
    public struct MessagePacket: NetworkMessage
    {
        public int MessageId;
        public string Type;
        public string Content;
    }
    private int LastMessageValue = 0;
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
        //Gameplay loops here
    }

    public void Testfunc()
    {
        string message = ClientAPI.Prepare_UPD(Globals.GetMap().GetComponent<MapRenderer>().Bounds, PlayerPrefs.GetString("Token", ""));
        SendMessageToServer("UPD", message);
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
        Globals.GetDebugConsole().LogMessage("Disconnected from server");
        Globals.GetLoader().Enter("Reconnecting");
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
            case "CHECK":
                Client_CHECK(msg);
                break;
            case "UPD":
                Client_UPD(msg);
                break;
            case "KILL":
                Client_KILL(msg);
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
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.msg;
        bool result = obj.success;

        if (result)
        {
            PlayerPrefs.SetString("Token", message);
            Globals.GetDebugConsole().LogMessage("REGISTER successful. New session token: " + message);
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Register Menu").GetComponent<RegisterMenu>().Exit();
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Startup Menu").GetComponent<StartupMenu>().Enter();
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetPrompt().ShowMessage(message);
            Globals.GetDebugConsole().LogMessage("REGISTER failed: "+message);
        }
    }

    private void Client_AUTH(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.msg;
        bool result = obj.success;

        if (result)
        {
            PlayerPrefs.SetString("Token", message);
            Globals.GetDebugConsole().LogMessage("AUTH successful!");
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Login Menu").GetComponent<LoginMenu>().Exit();
            GameObject.Find("Canvas").transform.Find("Startup View/Menus/Startup Menu").GetComponent<StartupMenu>().Enter();
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetPrompt().ShowMessage(message);
            Globals.GetDebugConsole().LogMessage("AUTH failed: " + message);
        }
    }

    private void Client_CHECK(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string message = obj.success?"":obj.msg;
        bool result = obj.success;

        if (result)
        {
            Globals.GetDebugConsole().LogMessage("CHECK successful!");
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetDebugConsole().LogMessage("CHECK failed: " + message);
        }
        Globals.GetLoader().Exit();
    }

    private void Client_UPD(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        List<GameplaySpot> spots = obj.spots.ToObject<List<GameplaySpot>>();
        PlayerData pd = obj.pd;

        foreach(GameplaySpot spot in spots)
        {
            GameObject newPin = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Spot Pin"), Globals.GetMap().transform);
            newPin.GetComponent<GameplaySpotInstance>().Init(spot);
        }

    }

    private void Client_KILL(MessagePacket msg)
    {
        //Get disconnected and die
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
            case "CHECK":
                Server_CHECK(conn, msg);
                break;
            case "UPD":
                Server_UPD(conn, msg);
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
                    if (Globals.GetDatabaseConnector().UserExists(ud)==0)
                    {
                        long userId = Globals.GetDatabaseConnector().InsertNewUser(ud);
                        long sessionId = Globals.GetDatabaseConnector().AddNewToken(newToken);
                        Globals.GetDatabaseConnector().AssignToken(sessionId, userId);
                        Globals.GetDatabaseConnector().ResetPlayerData(userId);

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
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string login = obj.login;
        string pass = obj.pass;

        long userId = Globals.GetDatabaseConnector().GetUserId(login);
        if (userId >= 0)
        {
            if (Globals.GetDatabaseConnector().CheckUserPassword(userId, pass))
            {
                string sessionToken = Globals.GetDatabaseConnector().FindExistingToken(userId);
                if (String.IsNullOrWhiteSpace(sessionToken) || Globals.GetDatabaseConnector().TokenInUse(sessionToken)!=1)
                {
                    sessionToken = GenerateToken();
                    if (String.IsNullOrWhiteSpace(sessionToken))
                    {
                        SendMessageToClient((NetworkConnectionToClient)conn, "AUTH", "{\"success\": false, \"msg\": \"Server error.\"}");
                        return;
                    }

                    long sessionId = Globals.GetDatabaseConnector().AddNewToken(sessionToken);
                    Globals.GetDatabaseConnector().AssignToken(sessionId, userId);
                }
                else
                {
                    Globals.GetDatabaseConnector().RefreshToken(sessionToken);
                }

                SendMessageToClient((NetworkConnectionToClient)conn, "AUTH", "{\"success\": true, \"msg\": \"" + sessionToken + "\"}");
            }
            else
            {
                SendMessageToClient((NetworkConnectionToClient)conn, "AUTH", "{\"success\": false, \"msg\": \"Wrong password.\"}");
            }
        }
        else
        {
            SendMessageToClient((NetworkConnectionToClient)conn, "AUTH", "{\"success\": false, \"msg\": \"No such user.\"}");
        }
    }

    private void Server_CHECK(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;

        bool result = Globals.GetDatabaseConnector().TokenInUse(token)==1;
        if (result)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);
            SendMessageToClient((NetworkConnectionToClient)conn, "CHECK", "{\"success\": true}");
        }
        else
        {
            SendMessageToClient((NetworkConnectionToClient)conn, "CHECK", "{\"success\": false, \"msg\": \"Token not in use or session timed out\"}");
        }
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

        if (Globals.GetDatabaseConnector().TokenInUse(token)==1)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);

            List<GameplaySpot> spots = Globals.GetDatabaseConnector().GetSpots();
            PlayerData pd = Globals.GetDatabaseConnector().GetPlayerData();

            dynamic resObj = new ExpandoObject();

            resObj.spots = spots.Where((x)=>bounds.Intersects(x.Coords)).ToArray();
            resObj.pd = pd;
            string message = JsonConvert.SerializeObject(resObj);

            SendMessageToClient((NetworkConnectionToClient)conn, "UPD", message);
        }
        else
        {
            SendMessageToClient((NetworkConnectionToClient)conn, "KILL", "{\"msg\": \"Connection timed out\"}");
        }
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
                Globals.GetDatabaseConnector().LogInDatabase("ServerErr", "Token generation failed");
                return "";
            }

            newToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 16);
            attempts++;
        } while (Globals.GetDatabaseConnector().TokenInUse(newToken)>0);

        return newToken;
    }
    #endregion
}
