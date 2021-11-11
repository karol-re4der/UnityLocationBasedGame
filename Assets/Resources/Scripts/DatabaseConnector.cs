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

public class DatabaseConnector : MonoBehaviour
{
    private IDbConnection dbcon;

    #region Misc
    public void ConnectToDatabase()
    {
        if (Globals.GetNetworkManager().mode==NetworkManagerMode.ServerOnly)
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
        if (dbcon==null)
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
        catch(Exception ex)
        {
            result = false;
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: "+ex.Message);
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
            if (reader!=null && reader.IsClosed)
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
            string query = $"DELETE FROM Sessions WHERE ValidUntil<datetime('now')";
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

    #endregion

    #region Gampeplay
    
    public List<GameplaySpot> GetSpots()
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        List<GameplaySpot> result = new List<GameplaySpot>();
        IDataReader reader = null;
        try
        {
            dbcon.Open();

            string query = "SELECT Name, Description, Value, Latitude, Longitude, OwnerId FROM Spots";
            IDbCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader[0].ToString();
                string desc = reader[1].ToString();
                int value = Int32.Parse(reader[2].ToString());
                double lat = double.Parse(reader[3].ToString());
                double lon = double.Parse(reader[4].ToString());
                LatLon coords = new LatLon(lat, lon);

                GameplaySpot nextSpot = new GameplaySpot
                {
                    Name = name,
                    Description = desc,
                    Value = value,
                    Coords = coords
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

    public PlayerData GetPlayerData()
    {
        return null;
    }

    public bool ResetPlayerData(long userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        bool success = false;
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
            query = $"INSERT INTO PlayerData(Value, LastLatitude, LastLongitude) VALUES({Globals.PlayerInitialValue}, 0, 0);SELECT last_insert_rowid();";
            dbcmd.CommandText = query;
            long playerDataId = (long)dbcmd.ExecuteScalar();

            //Assign to user
            query = $"UPDATE UserAccounts SET PlayerDataId={playerDataId} WHERE Id={userId}";
            dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Globals.GetDebugConsole().LogMessage("EXCEPTION on db connection: " + ex.Message);
            success = false;
        }
        finally
        {
            if (reader != null && reader.IsClosed)
            {
                reader.Close();
            }
            dbcon.Close();
        }

        return success;
    }

    #endregion
}
