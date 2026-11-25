using System;
using System.Collections.Generic;
using UnityEngine;

// Receives parsed commands and forwards them to the right SmartObject.
// One instance in the scene, referenced by the CommandServer.
public class CommandRouter : MonoBehaviour
{
    [SerializeField] private CommandServer server; // optional: found automatically if empty

    private readonly Dictionary<string, SmartObject> objects = new();

    // Ids of all registered objects (used by DebugCommands to build test buttons).
    public IReadOnlyCollection<string> KnownIds => objects.Keys;

    private void Awake()
    {
        if (server == null)
            server = FindAnyObjectByType<CommandServer>();

        // Register every SmartObject in the scene, including inactive ones.
        // FindObjectsByType without a sort mode is the current API (sort modes are deprecated).
        foreach (var obj in FindObjectsByType<SmartObject>(FindObjectsInactive.Include))
            Register(obj);
    }

    private void OnEnable() => SmartObject.StateChanged += OnStateChanged;
    private void OnDisable() => SmartObject.StateChanged -= OnStateChanged;

    public void Register(SmartObject obj)
    {
        if (string.IsNullOrEmpty(obj.Id))
        {
            Debug.LogWarning($"SmartObject on '{obj.name}' has no Id set, skipping registration.", obj);
            return;
        }
        if (objects.TryGetValue(obj.Id, out var existing) && existing != obj)
            Debug.LogWarning($"Duplicate SmartObject id '{obj.Id}' on '{obj.name}' (already used by '{existing.name}'). Overwriting.", obj);

        objects[obj.Id] = obj;
    }

    public void Unregister(SmartObject obj)
    {
        if (obj != null && objects.TryGetValue(obj.Id, out var existing) && existing == obj)
            objects.Remove(obj.Id);
    }

    public void HandleJson(string json)
    {
        var cmd = Command.FromJson(json);
        if (cmd == null || string.IsNullOrEmpty(cmd.id) || string.IsNullOrEmpty(cmd.action))
        {
            Debug.LogWarning($"Unparseable command: {json}");
            return;
        }

        if (objects.TryGetValue(cmd.id, out var obj) && obj != null)
            obj.Handle(cmd.action);
        else
            Debug.LogWarning($"No SmartObject with id '{cmd.id}'. Known ids: {string.Join(", ", objects.Keys)}");
    }

    // Full snapshot for an app that just connected.
    public string SnapshotJson()
    {
        var list = new List<StateMessage>();
        foreach (var kv in objects)
        {
            if (kv.Value == null) continue; // destroyed object
            list.Add(new StateMessage { id = kv.Key, state = kv.Value.IsOn ? "on" : "off" });
        }

        return JsonUtility.ToJson(new StateList { states = list.ToArray() });
    }

    private void OnStateChanged(SmartObject obj)
    {
        if (server == null) return;
        var msg = new StateMessage { id = obj.Id, state = obj.IsOn ? "on" : "off" };
        server.Send(JsonUtility.ToJson(msg));
    }

    [Serializable]
    private class StateList { public StateMessage[] states; }
}
