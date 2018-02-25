using SharpNet;
using SharpNet.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class SharpNetworking
{
    public static SharpNetworking Instance;
    #region Initialize
    #region Events
    private Action<SharpClient, int, byte[]> _OnMessageReceived;
    public Action<SharpClient> _OnClientConnected;
    private Action<SharpClient> _OnClientDisconnected;
    public Action<SharpNetworkEvent> _OnNetworkEvent;
    #endregion


    public static void Initialize(
        Action<SharpClient, int, byte[]> OnMessageReceived,
        Action<SharpClient> OnClientConnected,
        Action<SharpClient> OnClientDisconnected,
        Action<SharpNetworkEvent> OnNetworkEvent
        )
    {
        if (isinited)
            throw new Exception("Already Initialized");
        isinited = true;

        Instance = new SharpNetworking();

        Instance._OnMessageReceived = OnMessageReceived;
        Instance._OnClientConnected = OnClientConnected;
        Instance._OnClientDisconnected = OnClientDisconnected;
        Instance._OnNetworkEvent = OnNetworkEvent;

    }



    #endregion

    #region Variables
    #region isConnected
    private static bool isconnected = false;
    public static bool IsConnected
    {
        get
        {
            if (!isconnected)
                return false;
            if (_ishost && server.isRuning)
                return true;
            if (!_ishost && client.isConnected)
                return true;

            isconnected = false;
            return false;
        }
    }
    #endregion
    #region isHost
    private static bool _ishost = false;
    public static bool isHost { get { return _ishost; } }

    #endregion
    #region Initialize
    private static bool isinited = false;
    public static bool IsInitialized { get { return isinited; } }
    #endregion

    private static SharpTCPServer server = new SharpTCPServer();
    private static SharpTCPClient client = new SharpTCPClient();
    #endregion

    #region Host&Connect
    public static bool Host(int Port, SharpRoom room, out HostResult result)
    {
        if (IsConnected)
        {
            result = HostResult.AlreadyHosted;
            return false;
        }
        return _ishost = isconnected = server.Host(out result, Port, room);
    }
    public static bool Connect(string IP, int Port, string Password, SharpClient self, out ConnectResults result)
    {
        if (IsConnected)
        {
            result = ConnectResults.AlreadyConnected;
            return false;
        }
        bool succes = isconnected = client.Connect(IP, Port, Password, self, out result);
        _ishost = !succes;
        return succes;
    }
    #endregion

    #region Network Public Reads
    public static SharpRoom Room
    {
        get
        {
            if (!IsConnected)
                return null;
            if (_ishost)
                return server.Room;
            return client.Room;
        }
    }
    public static SharpClient Me
    {
        get
        {
            if (!IsConnected)
                return null;
            if (_ishost)
                return server.Self;
            return client.Self;
        }
    }

    internal Action<SharpClient, int, byte[]> OnMessageReceived { get => _OnMessageReceived; set => _OnMessageReceived = value; }
    internal Action<SharpClient> OnClientDisconnected { get => _OnClientDisconnected; set => _OnClientDisconnected = value; }

    #endregion

    #region Sending Message
    public static void SendMessage(int Channel, byte[] data, SharpTargets targets)
    {
        if (_ishost)
            server.SendMessage(Channel, data, targets);
        else
            client.SendMessage(Channel, data, targets);
    }
    public static void SendMessage(int Channel, byte[] data, SharpClient Targetclient)
    {
        if (_ishost)
            server.SendMessage(Channel, data, Targetclient);
        else
            client.SendMessage(Channel, data, Targetclient);
    }
    public static void SendMessage(int Channel, byte[] data, List<SharpClient> clients)
    {
        if (_ishost)
            server.SendMessage(Channel, data, clients);
        else
            client.SendMessage(Channel, data, clients);
    }

    #endregion
}