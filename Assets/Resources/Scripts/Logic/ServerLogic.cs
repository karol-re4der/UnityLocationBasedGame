using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Mirror;
using Microsoft.Geospatial;

public class ServerLogic : MonoBehaviour
{
    private DateTime nextTick;
    void Start()
    {
        if (!Globals.GetNetworkManager().IsHost)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (DateTime.Now > nextTick)
        {
            DateTime timestamp = DateTime.Now;
            if (Globals.GetMap()?.activeSelf == true && !Globals.GetLoader().IsOn())
            {
                nextTick = DateTime.Now.AddSeconds(Globals.IntervalInSeconds_UPD);

                List<long> users = Globals.GetDatabaseConnector().GetUserIds();

                foreach(long id in users)
                {
                    PlayerData pd = Globals.GetDatabaseConnector().GetPlayerData(id);

                    pd.IncomePerSecond = Globals.GetDatabaseConnector().CountPlayerIncome(id);
                    pd.Value += pd.IncomePerSecond * Globals.IntervalInSeconds_UPD;

                    Globals.GetDatabaseConnector().UpdatePlayerData(pd);
                }

                Globals.GetDatabaseConnector().LogInDatabase("TICK", "Tick completed in "+(DateTime.Now-timestamp).Milliseconds+"ms");
            }
        }
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

    public void Handle_REGISTER(NetworkConnectionToClient conn, UserData ud)
    {
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

                        Globals.GetNetworkManager().SendMessageToClient(conn, "REGISTER", "{\"success\": true, \"msg\": \"" + newToken + "\"}");
                    }
                    else
                    {
                        Globals.GetNetworkManager().SendMessageToClient(conn, "REGISTER", "{\"success\": false, \"msg\": \"Email or nickname already in use.\"}");
                    }
                }
                else
                {
                    Globals.GetNetworkManager().SendMessageToClient(conn, "REGISTER", "{\"success\": false, \"msg\": \"Server-error: cannot generate new token.\"}");
                }
            }
            else
            {
                Globals.GetNetworkManager().SendMessageToClient(conn, "REGISTER", "{\"success\": false, \"msg\": \"Password invalid.\"}");
            }
        }
        else
        {
            Globals.GetNetworkManager().SendMessageToClient(conn, "REGISTER", "{\"success\": false, \"msg\": \"Credentials incomplete.\"}");
        }
    }

    public void Handle_AUTH(NetworkConnectionToClient conn, string login, string pass)
    {
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
                        Globals.GetNetworkManager().SendMessageToClient(conn, "AUTH", "{\"success\": false, \"msg\": \"Server error.\"}");
                        return;
                    }

                    long sessionId = Globals.GetDatabaseConnector().AddNewToken(sessionToken);
                    Globals.GetDatabaseConnector().AssignToken(sessionId, userId);
                }
                else
                {
                    Globals.GetDatabaseConnector().RefreshToken(sessionToken);
                }

                Globals.GetNetworkManager().SendMessageToClient(conn, "AUTH", "{\"success\": true, \"msg\": \"" + sessionToken + "\"}");
            }
            else
            {
                Globals.GetNetworkManager().SendMessageToClient(conn, "AUTH", "{\"success\": false, \"msg\": \"Wrong password.\"}");
            }
        }
        else
        {
            Globals.GetNetworkManager().SendMessageToClient(conn, "AUTH", "{\"success\": false, \"msg\": \"No such user.\"}");
        }
    }

    public void Handle_CHECK(NetworkConnectionToClient conn, string token)
    {
        bool result = Globals.GetDatabaseConnector().TokenInUse(token) == 1;
        if (result)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);
            Globals.GetNetworkManager().SendMessageToClient(conn, "CHECK", "{\"success\": true}");
        }
        else
        {
            Globals.GetNetworkManager().SendMessageToClient(conn, "CHECK", "{\"success\": false, \"msg\": \"Token not in use or session timed out\"}");
        }
    }

    public void Handle_UPD(NetworkConnectionToClient conn, string token, GeoBoundingBox bounds)
    {
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
                    Globals.GetNetworkManager().KillClient(conn, "Server error");
                    return;
                }
            }
            if (spots == null)
            {
                Globals.GetNetworkManager().KillClient(conn, "Server error");
                return;
            }
            if (nonPlayers == null)
            {
                Globals.GetNetworkManager().KillClient(conn, "Server error");
                return;
            }

            SpotData[] spotsArr = spots.Where((x) => bounds.Intersects(x.Coords)).ToArray();
            NonPlayerData[] npArr = nonPlayers.Where((x) => bounds.Intersects(x.Coords)).ToArray();
            string message = ServerAPI.Prepare_UPD(spotsArr, npArr, pd);

            Globals.GetNetworkManager().SendMessageToClient(conn, "UPD", message);
        }
        else
        {
            Globals.GetNetworkManager().KillClient(conn, "Connection timed out");
        }
    }

    public void Handle_BUY(NetworkConnectionToClient conn, string token, long spotId)
    {
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
                                Globals.GetNetworkManager().SendMessageToClient(conn, "BUY", "{\"success\": true, \"msg\": " + spotId + "}");
                            }
                            else
                            {
                                Globals.GetNetworkManager().SendMessageToClient(conn, "BUY", "{\"success\": false, \"msg\": \"Cannot afford!\"}");
                            }
                        }
                        else
                        {
                            Globals.GetNetworkManager().SendMessageToClient(conn, "BUY", "{\"success\": false, \"msg\": \"Already owned!\"}");
                        }
                    }
                    else
                    {
                        Globals.GetNetworkManager().SendMessageToClient(conn, "BUY", "{\"success\": false, \"msg\": \"The spot does not exist!\"}");
                    }
                }
                else
                {
                    Globals.GetNetworkManager().KillClient(conn, "Server error", token);
                }
            }
            else
            {
                Globals.GetNetworkManager().KillClient(conn, "Server error", token);
            }
        }
        else
        {
            Globals.GetNetworkManager().KillClient(conn, "Connection timed out");
        }
    }

    public void Handle_WHOAMI(NetworkConnectionToClient conn, string token)
    {
        if (Globals.GetDatabaseConnector().TokenInUse(token) == 1)
        {
            Globals.GetDatabaseConnector().RefreshToken(token);

            long userId = Globals.GetDatabaseConnector().TokenToUserId(token);
            if (userId >= 0)
            {
                UserData ud = Globals.GetDatabaseConnector().GetUserData(userId);
                if (ud != null)
                {
                    string message = ServerAPI.Prepare_WHOAMI(ud);
                    Globals.GetNetworkManager().SendMessageToClient(conn, "WHOAMI", message);
                }
                else
                {
                    Globals.GetNetworkManager().KillClient(conn, "Server error", token);
                }
            }
            else
            {
                Globals.GetNetworkManager().KillClient(conn, "Server error");
            }
        }
        else
        {
            Globals.GetNetworkManager().KillClient(conn, "Connection timed out");
        }
    }
}
