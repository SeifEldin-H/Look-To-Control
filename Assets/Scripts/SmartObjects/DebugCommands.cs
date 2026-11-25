using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Lets you test without the remote app. Two ways:
//   1) Keys (click the Game view first): L = lamp, F = fan, T = TV, S = speaker, D = door lock
//   2) On-screen buttons in the Game view, one per registered SmartObject.
// On Play it logs which input backend is active and which ids are registered,
// and it logs every key press, so you can see where things go wrong.
public class DebugCommands : MonoBehaviour
{
    [SerializeField] private CommandRouter router;
    [SerializeField] private bool showButtons = true;

    [Header("Object ids (must match each SmartObject's Object Id)")]
    [SerializeField] private string lampId = "lamp_floor";
    [SerializeField] private string fanId = "fan_ceiling";
    [SerializeField] private string tvId = "tv_wall";
    [SerializeField] private string speakerId = "speaker";
    [SerializeField] private string doorLockId = "door_lock";

    private bool warnedNoKeyboard;

    private void Awake()
    {
        if (router == null)
            router = FindAnyObjectByType<CommandRouter>();
    }

    private void Start()
    {
        string backend = "";
#if ENABLE_INPUT_SYSTEM
        backend += "Input System (new) ";
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        backend += "Input Manager (legacy)";
#endif
        if (backend.Length == 0) backend = "NONE";

        if (router == null)
        {
            Debug.LogError("[DebugCommands] No CommandRouter found in the scene.", this);
            return;
        }

        Debug.Log($"[DebugCommands] Running. Input backend: {backend}. " +
                  $"Registered ids: {string.Join(", ", router.KnownIds)}", this);
    }

    private void Update()
    {
        if (router == null) return;

        bool lamp = false, fan = false, tv = false, speaker = false, door = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            lamp |= kb.lKey.wasPressedThisFrame;
            fan |= kb.fKey.wasPressedThisFrame;
            tv |= kb.tKey.wasPressedThisFrame;
            speaker |= kb.sKey.wasPressedThisFrame;
            door |= kb.dKey.wasPressedThisFrame;
        }
        else if (!warnedNoKeyboard)
        {
            warnedNoKeyboard = true;
            Debug.LogWarning("[DebugCommands] Keyboard.current is null: no keyboard detected, " +
                             "or the Game view isn't focused. Click inside the Game view.", this);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        lamp |= Input.GetKeyDown(KeyCode.L);
        fan |= Input.GetKeyDown(KeyCode.F);
        tv |= Input.GetKeyDown(KeyCode.T);
        speaker |= Input.GetKeyDown(KeyCode.S);
        door |= Input.GetKeyDown(KeyCode.D);
#endif

        if (lamp) Toggle(lampId, "L");
        if (fan) Toggle(fanId, "F");
        if (tv) Toggle(tvId, "T");
        if (speaker) Toggle(speakerId, "S");
        if (door) Toggle(doorLockId, "D");
    }

    // Clickable fallback: works even if keyboard input is misconfigured.
    private void OnGUI()
    {
        if (!showButtons || router == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 220, Screen.height - 20));
        GUILayout.Label("Smart objects (debug)");
        var ids = new List<string>(router.KnownIds);
        foreach (var id in ids)
        {
            if (GUILayout.Button($"Toggle {id}"))
                Toggle(id, "button");
        }
        GUILayout.EndArea();
    }

    private void Toggle(string id, string source)
    {
        Debug.Log($"[DebugCommands] {source} -> toggle '{id}'", this);
        router.HandleJson(JsonUtility.ToJson(new Command { id = id, action = "toggle" }));
    }
}
