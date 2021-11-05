using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;

public class DatabaseConnector : MonoBehaviour
{
    private IDbConnection dbcon;


    void Awake()
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
            gameObject.SetActive(false);
        }
    }

    public void LogInDatabase(string type, string content)
    {
        dbcon.Open();

        string query = "INSERT INTO Log (Type, Content, Timestamp) VALUES (\""+type+"\", \""+content+"\", \"" + DateTime.Now + "\")";
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;

        IDataReader reader = dbcmd.ExecuteReader();
        dbcon.Close();
    }
}
