using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using System.Data.SqlClient;

public class DatabaseConnector : MonoBehaviour
{
    private IDbConnection dbcon;


    public void ConnectToDatabase()
    {
        if (Globals.IsHost)
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

        string query = "INSERT INTO Log (Type, Content, Timestamp) VALUES (\""+type+"\", \""+content+"\", \"" + DateTime.Now + "\")";
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
}
