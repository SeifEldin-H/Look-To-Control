using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// A tiny dependency-free TCP server that speaks newline-delimited JSON.
// The remote app connects to this machine's IP on the chosen port and sends
// lines like:  {"id":"lamp_floor","action":"toggle"}
// Unity replies with state lines like:  {"id":"lamp_floor","state":"on"}
// and a full snapshot when a client first connects.
public class CommandServer : MonoBehaviour
{
    [SerializeField] private int port = 8787;
    [SerializeField] private CommandRouter router; // optional: found automatically if empty

    private TcpListener listener;
    private Thread acceptThread;
    private volatile bool running;

    private readonly ConcurrentQueue<string> inbound = new();
    private readonly ConcurrentQueue<string> outbound = new();
    private readonly ConcurrentQueue<TcpClient> newClients = new();
    private readonly ConcurrentDictionary<TcpClient, byte> clients = new();

    private void Awake()
    {
        if (router == null)
            router = FindAnyObjectByType<CommandRouter>();
        if (router == null)
        {
            Debug.LogError("CommandServer needs a CommandRouter in the scene.", this);
            enabled = false;
        }
    }

    // Start/stop with enable/disable (not just app quit) so the port is always released,
    // including in the editor with Domain Reload disabled.
    private void OnEnable()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
        }
        catch (SocketException e)
        {
            Debug.LogError($"CommandServer could not listen on port {port}: {e.Message}", this);
            listener = null;
            return;
        }

        running = true;
        acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "CommandServer.Accept" };
        acceptThread.Start();
        Debug.Log($"CommandServer listening on port {port}");
    }

    private void OnDisable()
    {
        running = false;
        try { listener?.Stop(); } catch { }
        listener = null;

        foreach (var client in clients.Keys)
            CloseClient(client);
        clients.Clear();

        while (inbound.TryDequeue(out _)) { }
        while (outbound.TryDequeue(out _)) { }
        while (newClients.TryDequeue(out _)) { }
    }

    private void AcceptLoop()
    {
        while (running)
        {
            TcpClient client;
            try { client = listener.AcceptTcpClient(); }
            catch { return; } // listener stopped

            client.NoDelay = true;
            client.SendTimeout = 1000; // a stuck client must not freeze the main thread

            clients[client] = 0;
            newClients.Enqueue(client);
            new Thread(() => ReadLoop(client)) { IsBackground = true, Name = "CommandServer.Read" }.Start();
        }
    }

    private void ReadLoop(TcpClient client)
    {
        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            string line;
            while (running && (line = reader.ReadLine()) != null)
                inbound.Enqueue(line);
        }
        catch { /* client disconnected */ }
        finally
        {
            clients.TryRemove(client, out _);
            CloseClient(client);
        }
    }

    // Drain queues on the main thread: Unity APIs may only be used there.
    private void Update()
    {
        while (inbound.TryDequeue(out var json))
            router.HandleJson(json);

        while (newClients.TryDequeue(out var client))
            WriteTo(client, router.SnapshotJson());

        while (outbound.TryDequeue(out var msg))
            WriteToAll(msg);
    }

    // Called by the router whenever an object changes state.
    public void Send(string message) => outbound.Enqueue(message);

    private void WriteToAll(string message)
    {
        foreach (var client in clients.Keys)
            WriteTo(client, message);
    }

    private void WriteTo(TcpClient client, string message)
    {
        if (!clients.ContainsKey(client)) return;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(message + "\n");
            client.GetStream().Write(bytes, 0, bytes.Length);
        }
        catch
        {
            clients.TryRemove(client, out _);
            CloseClient(client);
        }
    }

    private static void CloseClient(TcpClient client)
    {
        try { client.Close(); } catch { }
    }
}
