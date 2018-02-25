using SharpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpNet.Socket
{
    class SharpTCPClient
    {
        #region Variables
        string ip;
        int port;
        SharpRoom room = null;
        Thread listenerThread = null;
        TcpClient server_client;
        SharpClient me;
        bool connected = false;
        #endregion
        #region Main
        public SharpTCPClient() { }
        #endregion
        #region Public Vars
        public bool isConnected { get { return this.connected; } }
        public SharpClient Self { get { return this.me; } }
        public SharpClient Server { get { return this.room.Server; } }
        public SharpRoom Room { get { return this.room; } }

        #endregion
        #region Data Broadcasting
        public void SendMessage(int Channel, byte[] data, SharpTargets targets)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.MessageType1);

            ser.Write((byte)targets);
            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);

            Server.SendMessage(ser.DataAndPost());

        }
        public void SendMessage(int Channel, byte[] data, SharpClient client)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.MessageType2);

            ser.Write(client.NetworkID);
            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);

            Server.SendMessage(ser.DataAndPost());
        }
        public void SendMessage(int Channel, byte[] data, List<SharpClient> clients)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.MessageType3);


            ser.Write((short)clients.Count);
            foreach (var item in clients)
            {
                ser.Write(item.NetworkID);
            }

            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);

            Server.SendMessage(ser.DataAndPost());
        }

        #endregion
        #region File Tranfer
        public void Send_StartSendingFile(string fileName, int Size, List<SharpClient> clients)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.StartSendingFile);


        }
        public void Send_FileData(byte[] bytes, List<SharpClient> Destinations)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.StartSendingFile);
        }
        #endregion
        #region Connect
        public bool Connect(string IP, int Port, string Password, SharpClient Self, out ConnectResults result)
        {
            if (Password == null)
                Password = "";
            if (this.connected)
            {
                result = ConnectResults.AlreadyConnected;
                return false;
            }
            try
            {
                server_client = new TcpClient();
                IPAddress address = null;
                #region IP Parse
                try
                {
                    address = IPAddress.Parse(IP);
                }
                catch
                {
                    result = ConnectResults.InvalidIpAddress;
                    return false;
                }
                #endregion
                IPEndPoint remoteAddress = new IPEndPoint(address, Port);

                #region Physical Connetion
                try
                {
                    server_client.Connect(remoteAddress);
                }
                catch
                {
                    result = ConnectResults.UnhandledException;
                    return false;
                }
                #endregion

                #region ID Self
                SharpSerializer hailMessage = SharpSerializer.Create();

                hailMessage.Write((byte)IncomingConnectionRequests.ConnectionApprove);
                Self.Write(ref hailMessage);
                hailMessage.Write(Password);

                TCPMessageHandler.Write(server_client, hailMessage.DataAndPost());
                #endregion

                #region Wait For Response
                byte[] responseRaw = TCPMessageHandler.Read(server_client);
                SharpSerializer response = SharpSerializer.Create(responseRaw);
                #endregion

                IncomingConnectionResponse message = (IncomingConnectionResponse)response.ReadByte();
                if (message == IncomingConnectionResponse.WrongPassword)
                {
                    TCPMessageHandler.CloseConnection(server_client);
                    result = ConnectResults.InvalidPassword;
                    return false;
                }
                if (message == IncomingConnectionResponse.ServerIsFull)
                {
                    TCPMessageHandler.CloseConnection(server_client);
                    result = ConnectResults.ServerIsFull;
                    return false;
                }
                if (message == IncomingConnectionResponse.NameIsExist)
                {
                    TCPMessageHandler.CloseConnection(server_client);
                    result = ConnectResults.NameIsExist;
                    return false;
                }


                short myID = response.ReadInt16();
                Self.AssingNetID(myID);

                this.room = new SharpRoom(ref response);

                this.listenerThread = new Thread(ListenServer);
                this.listenerThread.Start();

                connected = true;

                me = Self;
                room.AssignClient(me);

                room.Server.Connection = server_client;
                room.Server.ListeningThread = listenerThread;

                C_OnConnected(room);

                result = ConnectResults.Succes;
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    TCPMessageHandler.CloseConnection(server_client);
                }
                catch { }
                server_client = null;

                connected = false;

                try
                {
                    listenerThread.Abort();
                }
                catch { }
                listenerThread = null;

                me = null;

                room = null;

                result = ConnectResults.UnhandledException;
                return false;
            }
        }
        void ListenServer()
        {
            while (true)
            {
                byte[] incomingPackage = null;
                try
                {
                    incomingPackage = TCPMessageHandler.Read(this.server_client);
                }
                catch
                {
                    C_OnDisconnected();
                    return;
                }
                C_OnReceivedMessage(incomingPackage);
            }
        }

        #endregion
        #region Disconnect / Reset
        public void Disconnect()
        {
            if (!connected)
                throw new Exception("You are not connected already");
            connected = false;


            ip = "";
            port = 0;
            room = null;
            me = null;


            try
            {
                TCPMessageHandler.CloseConnection(server_client);
            }
            catch { }
            try
            {
                listenerThread.Abort();
                listenerThread = null;
            }
            catch { }
        }


        #endregion



        #region Self Events
        void C_OnClientConnected(SharpClient client)
        {
            //Console.WriteLine("A Player connected : " + client.ClientName);
            SharpNetworking.Instance._OnClientConnected(client);
        }
        void C_OnClientDisconnected(SharpClient client)
        {
            //Console.WriteLine("A Player Disconnected : " + client.ClientName);
            SharpNetworking.Instance.OnClientDisconnected(client);
        }

        void C_OnConnected(SharpRoom room)
        {
            //Console.WriteLine("Connected To Server : Room name => " + room.RoomName);
            SharpNetworking.Instance._OnNetworkEvent(SharpNetworkEvent.Connected);
        }
        void C_OnDisconnected()
        {
            //Console.WriteLine("Disconnected From Server");
            SharpNetworking.Instance._OnNetworkEvent(SharpNetworkEvent.Disconnected);
        }

        void C_OnReceivedMessage(byte[] rawMessage)
        {
            SharpSerializer ser = SharpSerializer.Create(rawMessage);
            SharpConnectionMessages incomingType = (SharpConnectionMessages)ser.ReadByte();
            if (incomingType == SharpConnectionMessages.ClientConnected)
            {
                SharpClient incomingClient = new SharpClient(ref ser);
                this.room.AssignClient(incomingClient);
                C_OnClientConnected(incomingClient);
            }
            else if (incomingType == SharpConnectionMessages.ClientDisconnected)
            {
                short netID = ser.ReadInt16();
                C_OnClientDisconnected(this.room.Get(netID));
            }
            else if (incomingType == SharpConnectionMessages.DirectData)
            {
                short sender = ser.ReadShort();
                int channel = ser.ReadInt32();
                int packageSize = ser.ReadInt32();
                byte[] rawData = ser.ReadBytes(packageSize);
                C_OnReceivedData(this.room.Get(sender), channel, rawData);
            }

            ser.Post();
        }


        void C_OnReceivedData(SharpClient sender, int channel, byte[] data)
        {
            //Console.WriteLine(sender.ClientName + " : " + Encoding.UTF8.GetString(data));
            SharpNetworking.Instance.OnMessageReceived(sender, channel, data);
        }
        #endregion
    }
}
