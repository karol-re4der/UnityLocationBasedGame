using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using System.Data.SqlClient;
using Microsoft.Geospatial;
using Mirror;
using System.Globalization;

public class DatabaseConnector : MonoBehaviour
{
    private IDbConnection dbcon;

    #region Misc
    public void ConnectToDatabase()
    {
        if (Globals.GetNetworkManager().mode == NetworkManagerMode.ServerOnly)
        {
            try
            {
                dbcon = new SqliteConnection(Globals.SqliteConnectionString);

                LogInDatabase("DBConn", "Server connected to the database");

                Globals.GetDebugConsole().LogMessage("Database connected");
            }
            catch (SqliteException ex)
            {
                Globals.GetDebugConsole().LogMessage("Cannot connect to database");
                Application.Quit(); //Not working?
            }
        }
        else
        {
            this.enabled = false;
        }
    }
    public bool LogInDatabase(string type, string content)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        bool result = true;
        try
        {
            dbcon.Open();

            string query = "INSERT INTO Log (Type, Content, Timestamp) VALUES ('" + type + "', '" + content + "', '" + DateTime.Now + "')";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            result = false;
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            dbcon.Close();
        }


        return result;
    }
    public int GetNextMessageId()
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        int value = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            //Get ID
            string query = "SELECT Id, Value, Step FROM Counters WHERE Name=\"MessageId\"";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            value = Int32.Parse(reader[1].ToString());
            int step = Int32.Parse(reader[2].ToString());
            reader.Close();

            //Increment
            query = "UPDATE Counters SET Value=Value+" + step + " WHERE Id=" + id;
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return value;
    }

    #endregion

    #region Accounts

    public long InsertNewUser(UserData user)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        long userId = -1;
        try
        {
            dbcon.Open();

            //Insert user
            string query = $"INSERT INTO UserAccounts(Name, Surname, Nickname, Email, Password) VALUES('{user.Name}','{user.Surname}','{user.Nickname}','{user.Email}','{user.Password}');SELECT last_insert_rowid();";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            userId = (long)dbcmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            dbcon.Close();
        }

        return userId;
    }
    public int UserExists(UserData ud)
    {
        //Return values legend:
        //0 - not existing
        //1 - exists
        //Other - something went wrong

        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        int count = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT Count(*) FROM UserAccounts WHERE Email='{ud.Email}' OR Nickname='{ud.Nickname}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            count = Int32.Parse(reader[0].ToString());
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return count;
    }
    public bool CheckUserPassword(long userId, string pass)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        string password = "";
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT password FROM UserAccounts WHERE Id='{userId}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            password = reader[0].ToString();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return String.IsNullOrWhiteSpace(pass) ? false : password.Equals(pass);
    }
    public long GetUserId(string login)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        long id = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT Id FROM UserAccounts WHERE Nickname='{login}' OR Email='{login}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            id = long.Parse(reader[0].ToString());
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return id;
    }
    public UserData GetUserData(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        UserData ud = null;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT Name, Surname, Nickname, Email FROM UserAccounts WHERE Id='{userId}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader[0].ToString();
                string surname = reader[1].ToString();
                string nickname = reader[2].ToString();
                string email = reader[3].ToString();

                ud = new UserData
                {
                    Name = name,
                    Surname = surname,
                    Nickname = nickname,
                    Email = email
                };
            }
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return ud;
    }

    #endregion

    #region Token

    public bool RefreshToken(string token)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        bool result = true;
        try
        {
            dbcon.Open();

            string query = $"UPDATE Sessions SET LastUsed=datetime('now'), ValidUntil=datetime('now', '+{(Globals.SessionTimeoutInHours)} hour') WHERE Token='{token}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            result = false;
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            dbcon.Close();
        }

        return result;
    }
    public string FindExistingToken(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        string token = "";
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT s.Token FROM Sessions s JOIN UserAccounts a ON a.SessionId=s.Id WHERE a.Id={userId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            token = reader[0].ToString();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return token;
    }
    public int TokenInUse(string token)
    {
        //Return values legend:
        //0 - not in use
        //>0 in use
        //Other - something went wrong

        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        int count = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            //Clean timed out sessions
            string query = $"UPDATE UserAccounts SET SessionId=NULL WHERE SessionId in (SELECT Id FROM Sessions WHERE ValidUntil<datetime('now')); " +
                           $"DELETE FROM Sessions WHERE ValidUntil<datetime('now');";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();

            //Get session
            query = "SELECT Count(*) FROM Sessions WHERE Token='" + token + "'";
            dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            reader.Read();
            count = Int32.Parse(reader[0].ToString());
            reader.Close();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return count;
    }
    public bool AssignToken(long sessionId, long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        try
        {
            dbcon.Open();

            string query = $"UPDATE UserAccounts SET SessionId={sessionId} WHERE Id={userId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return false;
        }
        finally
        {
            dbcon.Close();
        }

        return true;
    }
    public long AddNewToken(string token)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        long sessionId = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            //Insert new session
            string query = $"INSERT INTO Sessions(Token, LastUsed, ValidUntil) VALUES('{token}', datetime('now'), datetime('now', '+{(Globals.SessionTimeoutInHours)} hour'));SELECT last_insert_rowid();";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            sessionId = (long)dbcmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return sessionId;
    }
    public long TokenToUserId(string token)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        long userId = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            //Insert new session
            string query = $"SELECT ua.Id FROM UserAccounts ua LEFT JOIN Sessions se ON ua.SessionId=se.Id WHERE se.Token='{token}'";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            userId = (long)dbcmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return userId;
    }
    #endregion

    #region Gameplay

    public List<SpotData> GetSpots()
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        List<SpotData> result = new List<SpotData>();
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = "SELECT sp.Id, sp.Name, sp.Description, sp.Value, sp.IncomePerSecond, sp.Latitude, sp.Longitude, sp.OwnerId, ua.Nickname FROM Spots sp LEFT JOIN UserAccounts ua ON ua.Id=sp.OwnerId";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                long id = long.Parse(reader[0].ToString());
                string name = reader[1].ToString();
                string desc = reader[2].ToString();
                int value = Int32.Parse(reader[3].ToString());
                int incomePerSecond = Int32.Parse(reader[4].ToString());
                double lat = double.Parse(reader[5].ToString());
                double lon = double.Parse(reader[6].ToString());
                long ownerId = -1;
                string ownerNickname = "";
                if (long.TryParse(reader[7].ToString(), out ownerId))
                {
                    ownerNickname = reader[8].ToString();
                }

                SpotData nextSpot = new SpotData
                {
                    Id = id,
                    Name = name,
                    Description = desc,
                    Value = value,
                    IncomePerSecond = incomePerSecond,
                    Lat = lat,
                    Lon = lon,
                    OwnerId = ownerId,
                    OwnerNickname = ownerNickname
                };
                result.Add(nextSpot);
            }
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return result;
    }
    public PlayerData GetPlayerData(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        PlayerData result = null;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT pd.Id, pd.Value, pd.IncomePerSecond FROM UserAccounts ua JOIN PlayerData pd ON ua.PlayerDataId=pd.Id WHERE ua.Id={userId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                long id = long.Parse(reader[0].ToString());
                int value = Int32.Parse(reader[1].ToString());
                int income = Int32.Parse(reader[2].ToString());

                result = new PlayerData
                {
                    PlayerDataId = id,
                    Value = value,
                    IncomePerSecond = income
                };
            }
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return result;
    }
    public List<NonPlayerData> GetNonPlayers(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        List<NonPlayerData> result = new List<NonPlayerData>();
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT ua.Id, pd.LastLatitude, pd.LastLongitude FROM UserAccounts ua INNER JOIN Sessions se ON ua.SessionId = se.Id INNER JOIN PlayerData pd ON ua.PlayerDataId = pd.Id WHERE se.LastUsed > datetime('now', '-{(Globals.NonPlayerVisibilityInSeconds)} second') AND ua.Id !={ userId} AND (pd.LastLatitude!=0 AND pd.LastLongitude!=0)";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                long id = long.Parse(reader[0].ToString());
                double lat = double.Parse(reader[1].ToString());
                double lon = double.Parse(reader[2].ToString());

                NonPlayerData nextNonPlayer = new NonPlayerData
                {
                    UserId = id,
                    Lat = lat,
                    Lon = lon
                };
                result.Add(nextNonPlayer);
            }
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return result;
    }
    public bool UpdatePlayerData(PlayerData newData)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        try
        {
            dbcon.Open();

            string query = $"UPDATE PlayerData SET Value={newData.Value}, IncomePerSecond={newData.IncomePerSecond} WHERE Id={newData.PlayerDataId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return false;
        }
        finally
        {
            dbcon.Close();
        }

        return true;
    }
    public bool UpdatePlayerPos(long playerDataId, LatLon coords)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        try
        {
            dbcon.Open();

            string lat = coords.LatitudeInDegrees.ToString("0.0000000000000", CultureInfo.InvariantCulture);
            string lon = coords.LongitudeInDegrees.ToString("0.0000000000000", CultureInfo.InvariantCulture);
            string query = $"UPDATE PlayerData SET LastLatitude={lat}, LastLongitude={lon} WHERE Id={playerDataId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return false;
        }
        finally
        {
            dbcon.Close();
        }

        return true;
    }
    public List<long> GetUserIds()
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        List<long> result = new List<long>();
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = "SELECT Id FROM UserAccounts";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                long id = long.Parse(reader[0].ToString());
                result.Add(id);
            }
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            result = null;
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return result;
    }
    public long ResetPlayerData(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        long playerDataId = -1;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            //Remove existing PlayerData
            string query = $"DELETE FROM PlayerData WHERE ROWID IN (SELECT a.ROWID FROM PlayerData a INNER JOIN UserAccounts b ON (a.Id=b.PlayerDataId ) WHERE b.Id={userId});";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();

            //Insert PlayerData
            query = $"INSERT INTO PlayerData(Value, IncomePerSecond, LastLatitude, LastLongitude) VALUES({Globals.PlayerInitialValue}, 0, 0, 0);SELECT last_insert_rowid();";
            dbcmd.CommandText = query;
            playerDataId = (long)dbcmd.ExecuteScalar();

            //Assign to user
            query = $"UPDATE UserAccounts SET PlayerDataId={playerDataId} WHERE Id={userId}";
            dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return playerDataId;
    }
    public int CountPlayerIncome(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        int income = 0;
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = $"SELECT IFNULL(SUM(IncomePerSecond), 0)+{Globals.PlayerBaseIncome} FROM Spots WHERE OwnerId={userId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();

            income = Int32.Parse(reader[0].ToString());
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return 0;
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return income;
    }
    public bool ChargePlayer(long userId, int value)
    {
        return true;
    }
    public SpotData GetSpot(long spotId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        SpotData result = new SpotData();
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = "SELECT sp.Id, sp.Name, sp.Description, sp.Value, sp.IncomePerSecond, sp.Latitude, sp.Longitude, sp.OwnerId, ua.Nickname FROM Spots sp LEFT JOIN UserAccounts ua ON ua.Id=sp.OwnerId";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();

            long id = long.Parse(reader[0].ToString());
            string name = reader[1].ToString();
            string desc = reader[2].ToString();
            int value = Int32.Parse(reader[3].ToString());
            int incomePerSecond = Int32.Parse(reader[4].ToString());
            double lat = double.Parse(reader[5].ToString());
            double lon = double.Parse(reader[6].ToString());
            long ownerId = -1;
            string ownerNickname = "";
            if (long.TryParse(reader[7].ToString(), out ownerId))
            {
                ownerNickname = reader[8].ToString();
            }

            result = new SpotData
            {
                Id = id,
                Name = name,
                Description = desc,
                Value = value,
                IncomePerSecond = incomePerSecond,
                Lat = lat,
                Lon = lon,
                OwnerId = ownerId,
                OwnerNickname = ownerNickname
            };
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return null;
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return result;
    }
    public bool UpdateSpotOwner(long userId, long spotId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        try
        {
            dbcon.Open();

            string query = $"UPDATE Spots SET OwnerId={userId} WHERE Id={spotId}";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            return false;
        }
        finally
        {
            dbcon.Close();
        }

        return true;
    }

    #endregion
}
