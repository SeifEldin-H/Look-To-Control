using System;
using UnityEngine; // required for JsonUtility (was missing -> compile error)

// The message the remote app sends: {"id":"lamp_floor","action":"toggle"}
[Serializable]
public class Command
{
    public string id;
    public string action; // "on" | "off" | "toggle"

    public static Command FromJson(string json)
    {
        try { return JsonUtility.FromJson<Command>(json); }
        catch (Exception) { return null; }
    }
}

// The message Unity sends back after a state change: {"id":"lamp_floor","state":"on"}
[Serializable]
public class StateMessage
{
    public string id;
    public string state; // "on" | "off"
}
