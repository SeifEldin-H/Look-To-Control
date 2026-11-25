using System;
using UnityEngine;

// Base class for every activatable living-room object.
// Subclasses implement what "on" and "off" actually look like.
public abstract class SmartObject : MonoBehaviour
{
    [SerializeField] private string objectId;

    public string Id => objectId;
    public bool IsOn { get; protected set; }

    // Fired on every state change; the router listens and pushes to the app.
    public static event Action<SmartObject> StateChanged;

    // Static state survives play sessions when "Enter Play Mode Options > Reload Domain"
    // is off, which would leave stale subscribers. Reset it on every play start.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => StateChanged = null;

    public void Handle(string action)
    {
        // Redundant commands (e.g. "on" while already on) are ignored so sounds,
        // videos and animations don't restart for no reason.
        switch (action)
        {
            case "on": if (!IsOn) TurnOn(); break;
            case "off": if (IsOn) TurnOff(); break;
            case "toggle": if (IsOn) TurnOff(); else TurnOn(); break;
            default:
                Debug.LogWarning($"[{name}] Unknown action '{action}'", this);
                break;
        }
    }

    protected virtual void TurnOn()
    {
        IsOn = true;
        ReportState();
    }

    protected virtual void TurnOff()
    {
        IsOn = false;
        ReportState();
    }

    protected void ReportState()
    {
        StateChanged?.Invoke(this);
        Debug.Log($"[{Id}] is now {(IsOn ? "ON" : "OFF")}", this);
    }

    // Default the id to the GameObject's name in the editor.
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(objectId))
            objectId = name.ToLowerInvariant().Replace(' ', '_');
    }
}
