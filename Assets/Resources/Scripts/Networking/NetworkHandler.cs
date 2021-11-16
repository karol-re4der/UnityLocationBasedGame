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
        public int MessageId;
        public string Type;
        public string Content;
    }
    private bool _beenConnected = false;
    private int _lastMessageValue = 0;
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
            case "WHOAMI":
                Client_WHOAMI(msg);
                break;
            case "BUY":
                Client_BUY(msg);
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
            Globals.GetDebugConsole().LogMessage("REGISTER failed: " + message);
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
        string message = obj.success ? "" : obj.msg;
        bool result = obj.success;

        if (result)
        {
            Globals.GetDebugConsole().LogMessage("CHECK successful!");
            Globals.GetStartupManager().EnterGameView();
        }
        else
        {
            Globals.GetDebugConsole().LogMessage("CHECK failed: " + message);

            if (Globals.GetMap().activeSelf)
            {
                Globals.GetStartupManager().ExitGameView();
                Globals.GetPrompt().ShowMessage("Disconnected! " + message);
            }
        }
        Globals.GetLoader().Exit();
    }

    private void Client_UPD(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        List<SpotData> spots = obj.spots.ToObject<List<SpotData>>();
        List<NonPlayerData> nonPlayers = obj.nonPlayers.ToObject<List<NonPlayerData>>();
        Globals.GetClientLogic().LatestPlayerData = obj.pd.ToObject<PlayerData>();
        Globals.GetClientLogic().LatestPlayerData.Init();

        List<SpotPin> existingSpots = Globals.GetMap().GetComponentsInChildren<SpotPin>().ToList<SpotPin>();
        List<NonPlayerPin> existingNonPlayers = Globals.GetMap().GetComponentsInChildren<NonPlayerPin>().ToList<NonPlayerPin>();


        //Delete out of range spots
        foreach (SpotPin spot in existingSpots)
        {
            if (!spots.Exists((x) => x.Id == spot.Data.Id))
            {
                GameObject.Destroy(spot.gameObject);
            }
        }

        //Instantiate new in range spots
        foreach (SpotData spot in spots)
        {
            if (!existingSpots.Exists((x) => x.Data.Id == spot.Id))
            {
                GameObject newPin = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Spot Pin"), Globals.GetMap().transform);
                newPin.GetComponent<SpotPin>().Init(spot);
            }
        }

        //Delete outdated/outranged players 
        foreach (NonPlayerPin np in existingNonPlayers)
        {
            if (!nonPlayers.Exists((x) => x.UserId == np.Data.UserId))
            {
                GameObject.Destroy(np.gameObject);
            }
        }

        //Instantiate new players in range OR update the position
        foreach (NonPlayerData nonPlayer in nonPlayers)
        {
            if (!existingNonPlayers.Exists((x) => x.Data.UserId == nonPlayer.UserId))
            {
                GameObject newNonPlayer = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player Pin"), Globals.GetMap().transform);
                newNonPlayer.GetComponent<NonPlayerPin>().Init(nonPlayer);
            }
            else if (existingNonPlayers.Exists((x) => x.Data.UserId == nonPlayer.UserId))
            {
                existingNonPlayers.Find((x) => x.Data.UserId == nonPlayer.UserId).Init(nonPlayer);
            }
        }
    }

    private void Client_KILL(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string reason = obj.reason;

        Globals.GetPrompt().ShowMessage("Disconnected by server. " + reason);

        NetworkClient.Disconnect();
        if (Globals.GetMap().activeSelf)
        {
            Globals.GetStartupManager().ExitGameView();
        }
    }

    private void Client_BUY(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        bool result = obj.success;
        long spotId = result ? obj.msg : -1;
        string message = result ? "" : obj.msg;

        if (result)
        {
            Globals.GetPrompt().ShowMessage("Purchase successful!");
            Globals.GetDebugConsole().LogMessage("BUY successful!");

            SpotPin spot = Globals.GetMap().GetComponentsInChildren<SpotPin>().ToList<SpotPin>().Find((x) => x.Data.Id == spotId);
            spot.Data.OwnerId = spotId;
            if (Globals.GetClientLogic().LatestUserData != null)
            {
                spot.Data.OwnerNickname = Globals.GetClientLogic().LatestUserData.Nickname;
            }
            if (Globals.GetClientLogic().LatestPlayerData.Value != null)
            {
                Globals.GetClientLogic().LatestPlayerData.IncomePerSecond += spot.Data.IncomePerSecond;
                Globals.GetClientLogic().LatestPlayerData.ValueUpdated -= spot.Data.Value;
            }
            if (Globals.GetSpotMenu().IsOn())
            {
                Globals.GetSpotMenu().Enter(spot.Data);
            }
        }
        else
        {
            Globals.GetPrompt().ShowMessage("Purchase failed! " + message);
            Globals.GetDebugConsole().LogMessage("BUY failed: " + message);
        }
    }

    private void Client_WHOAMI(MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        UserData ud = obj.ud.ToObject<UserData>();

        Globals.GetClientLogic().LatestUserData = ud;
    }

    public void SendMessageToServer(string type, string content)
    {
        _lastMessageValue++;
        MessagePacket msg = new MessagePacket
        {
            MessageId = _lastMessageValue,
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
        string debugText = "Message " + msg.MessageId + " from " + conn.address + " received. Content: " + msg.Content;
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
                    if (Globals.GetDatabaseConnector().UserExists(ud) == 0)
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
                if (String.IsNullOrWhiteSpace(sessionToken) || Globals.GetDatabaseConnector().TokenInUse(sessionToken) != 1)
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

        bool result = Globals.GetDatabaseConnector().TokenInUse(token) == 1;
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

        if (Globals.GetDatabaseConnector().TokenInUse(token) == 1)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);
            long userId = Globals.GetDatabaseConnector().TokenToUserId(token);

            List<SpotData> spots = Globals.GetDatabaseConnector().GetSpots();
            List<NonPlayerData> nonPlayers = Globals.GetDatabaseConnector().GetNonPlayers(userId);
            PlayerData pd = Globals.GetDatabaseConnector().GetPlayerData(userId);

            if (bounds.Center.LatitudeInDegrees != 0 && bounds.Center.LongitudeInDegrees != 0)
            {
                if (!Globals.GetDatabaseConnector().UpdatePlayerPos(pd.PlayerDataId, bounds.Center))
                {
                    KillClient((NetworkConnectionToClient)conn, "Server error");
                    return;
                }
            }
            if (spots == null)
            {
                KillClient((NetworkConnectionToClient)conn, "Server error");
                return;
            }
            if (nonPlayers == null)
            {
                KillClient((NetworkConnectionToClient)conn, "Server error");
                return;
            }

            dynamic resObj = new ExpandoObject();

            resObj.spots = spots.Where((x) => bounds.Intersects(x.Coords)).ToArray();
            resObj.nonPlayers = nonPlayers.Where((x) => bounds.Intersects(x.Coords)).ToArray();
            resObj.pd = pd;
            string message = JsonConvert.SerializeObject(resObj);

            SendMessageToClient((NetworkConnectionToClient)conn, "UPD", message);
        }
        else
        {
            KillClient((NetworkConnectionToClient)conn, "Connection timed out");
        }
    }

    private void Server_WHOAMI(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;

        if (Globals.GetDatabaseConnector().TokenInUse(token) == 1)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);

            long userId = Globals.GetDatabaseConnector().TokenToUserId(token);
            if (userId >= 0)
            {
                UserData ud = Globals.GetDatabaseConnector().GetUserData(userId);
                if (ud != null)
                {
                    dynamic resObj = new ExpandoObject();
                    resObj.ud = ud;
                    string message = JsonConvert.SerializeObject(resObj);
                    SendMessageToClient((NetworkConnectionToClient)conn, "WHOAMI", message);
                }
                else
                {
                    KillClient((NetworkConnectionToClient)conn, "Server error", token);
                }
            }
            else
            {
                KillClient((NetworkConnectionToClient)conn, "Server error");
            }
        }
        else
        {
            KillClient((NetworkConnectionToClient)conn, "Connection timed out");
        }
    }

    private void Server_BUY(NetworkConnection conn, MessagePacket msg)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(msg.Content);
        string token = obj.token;
        long spotId = obj.spotId;

        if (Globals.GetDatabaseConnector().TokenInUse(token) == 1)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);

            long userId = Globals.GetDatabaseConnector().TokenToUserId(token);
            if (userId >= 0)
            {
                PlayerData pd = Globals.GetDatabaseConnector().GetPlayerData(userId);
                if (pd != null)
                {
                    SpotData sd = Globals.GetDatabaseConnector().GetSpot(spotId);
                    if (sd != null)
                    {
                        if (sd.OwnerId != userId)
                        {
                            if (Globals.GetDatabaseConnector().ChargePlayer(userId, sd.Value))
                            {
                                Globals.GetDatabaseConnector().UpdateSpotOwner(userId, spotId);
                                SendMessageToClient((NetworkConnectionToClient)conn, "BUY", "{\"success\": true, \"msg\": " + spotId + "}");
                            }
                            else
                            {
                                SendMessageToClient((NetworkConnectionToClient)conn, "BUY", "{\"success\": false, \"msg\": \"Cannot afford!\"}");
                            }
                        }
                        else
                        {
                            SendMessageToClient((NetworkConnectionToClient)conn, "BUY", "{\"success\": false, \"msg\": \"Already owned!\"}");
                        }
                    }
                    else
                    {
                        SendMessageToClient((NetworkConnectionToClient)conn, "BUY", "{\"success\": false, \"msg\": \"The spot does not exist!\"}");
                    }
                }
                else
                {
                    KillClient((NetworkConnectionToClient)conn, "Server error", token);

                }
            }
            else
            {
                KillClient((NetworkConnectionToClient)conn, "Server error", token);
            }
        }
        else
        {
            KillClient((NetworkConnectionToClient)conn, "Connection timed out");
        }
    }

    private void Server_KILL(NetworkConnection conn, MessagePacket msg)
    {
        KillClient((NetworkConnectionToClient)conn, "Wrong request", msg.Content);
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
        Globals.GetDatabaseConnector().LogInDatabase("MSG", $"Message of type {type} sent to {conn.address}, content: {text}");
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
        } while (Globals.GetDatabaseConnector().TokenInUse(newToken) > 0);

        return newToken;
    }

    private void KillClient(NetworkConnectionToClient conn, string reason, string token = "")
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
