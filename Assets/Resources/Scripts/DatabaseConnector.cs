using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using System.Data.SqlClient;
using Mirror;

public class DatabaseConnector : MonoBehaviour
{
    private IDbConnection dbcon;


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

    public void LogInDatabase(string type, string content)
    {
        if (dbcon==null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = "INSERT INTO Log (Type, Content, Timestamp) VALUES ('"+type+"', '"+content+"', '" + DateTime.Now + "')";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;

        dbcmd.ExecuteNonQuery();
        dbcon.Close();
    }

    public int GetNextMessageId()
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = "SELECT Id, Value, Step FROM Counters WHERE Name=\"MessageId\"";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;

        IDataReader reader = dbcmd.ExecuteReader();
        reader.Read();
        int id = Int32.Parse(reader[0].ToString());
        int value = Int32.Parse(reader[1].ToString());
        int step = Int32.Parse(reader[2].ToString());
        reader.Close();

        query = "UPDATE Counters SET Value=Value+"+step+" WHERE Id="+id;
        dbcmd.CommandText = query;
        dbcmd.ExecuteNonQuery();

        dbcon.Close();

        return value;
    }

    public bool TokenInUse(string token)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = "SELECT Count(*) FROM Sessions WHERE Token='"+token+"'";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;

        IDataReader reader = dbcmd.ExecuteReader();
        reader.Read();
        int count = Int32.Parse(reader[0].ToString());
        reader.Close();

        dbcon.Close();

        return count>0;
    }

    public int InsertNewSession(string token)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"INSERT INTO Sessions(Token, ValidUntil, LastUsed) VALUES('{token}', '{DateTime.Now}', '{DateTime.Now.AddDays(7)}')";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        dbcmd.ExecuteNonQuery();

        query = $"SELECT Id FROM Sessions WHERE Token='{token}'";
        dbcmd.CommandText = query;
        IDataReader reader = dbcmd.ExecuteReader();
        reader.Read();
        int sessionId = Int32.Parse(reader[0].ToString());
        reader.Close();

        dbcon.Close();

        return sessionId;
    }

    public int InsertNewUser(UserData user)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"INSERT INTO UserAccounts(Name, Surname, Nickname, Email, Password) VALUES('{user.Name}','{user.Surname}','{user.Nickname}','{user.Email}','{user.Password}')";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        dbcmd.ExecuteNonQuery();

        query = $"SELECT Id FROM UserAccounts WHERE Nickname='{user.Nickname}'";
        dbcmd.CommandText = query;
        IDataReader reader = dbcmd.ExecuteReader();
        reader.Read();
        int userId = Int32.Parse(reader[0].ToString());
        reader.Close();

        dbcon.Close();
        return userId;
    }

    public bool UserExists(UserData ud)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"SELECT Count(*) FROM UserAccounts WHERE Email='{ud.Email}' OR Nickname='{ud.Nickname}'";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;

        IDataReader reader = dbcmd.ExecuteReader();
        reader.Read();
        int count = Int32.Parse(reader[0].ToString());

        reader.Close();
        dbcon.Close();

        return count > 0;
    }

    public void AssignSession(int sessionId, int userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"UPDATE UserAccounts SET SessionId={sessionId} WHERE Id={userId}";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        dbcmd.ExecuteNonQuery();

        dbcon.Close();
    }

    public bool CheckUserPassword(int userId, string pass)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"SELECT password FROM UserAccounts WHERE Id='{userId}'";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        IDataReader reader = dbcmd.ExecuteReader();

        string password = "";
        try
        {
            reader.Read();
            password = reader[0].ToString();
        }
        catch (SqliteException ex)
        {
            
        }

        reader.Close();
        dbcon.Close();
        return String.IsNullOrWhiteSpace(pass)?false:password.Equals(pass);
    }

    public int FindUser(string login)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"SELECT Id FROM UserAccounts WHERE Nickname='{login}' OR Email='{login}'";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        IDataReader reader = dbcmd.ExecuteReader();

        int id = -1;
        try
        {
            reader.Read();
            id = Int32.Parse(reader[0].ToString());
        }
        catch (SqliteException ex)
        {

        }

        reader.Close();
        dbcon.Close();
        return id;
    }

    public string FindExistingToken(int userId)
    {
        if (dbcon == null)
        {
            ConnectToDatabase();
        }

        dbcon.Open();

        string query = $"SELECT s.Token FROM Sessions s JOIN UserAccounts a ON a.SessionId=s.Id WHERE a.Id={userId}";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        IDataReader reader = dbcmd.ExecuteReader();

        string token = "";
        try
        {
            reader.Read();
            token = reader[0].ToString();
        }
        catch (SqliteException ex)
        {

        }

        reader.Close();
        dbcon.Close();
        return token;
    }
}
