using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UserData
{
    public String Name = "";
    public String Surname = "";
    public String Nickname = "";
    public String Email = "";

    public bool IsComplete()
    {
        return String.IsNullOrWhiteSpace(Name) || String.IsNullOrWhiteSpace(Surname) || String.IsNullOrWhiteSpace(Nickname) || String.IsNullOrWhiteSpace(Email) || !Email.Contains("@");
    }
}
