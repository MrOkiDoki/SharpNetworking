using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpNet.Socket
{
    class SharpTCPServer
    {
        #region Variables
        int port;
        bool hosted = false;
        int timeout = 5000;

        SharpRoom room;
        TcpListener listener;
        SharpClient serverclient;
        Thread listenerThread;
        #endregion
        #region Main
        public SharpTCPServer()
        {
        }
        #endregion
        #region Host
        public bool Host(out HostResult result, int Port, SharpRoom room)
        {

            if (hosted)
            {
                result = HostResult.AlreadyHosted;
                return false;
            }
            try
            {
                this.port = Port;
                this.room = room;

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, this.port);
                listener = new TcpListener(endPoint);
                listener.Start();

                serverclient = new SharpClient("ServerClient");
                room.AssignClient(serverclient);

                listenerThread = new Thread(ListenClients);
                listenerThread.Start();

                hosted = true;

                S_OnHosted();
                result = HostResult.Succes;
                return true;
            }
            catch (Exception e)
            {
                try { listener.Stop(); } catch { }
                try { listenerThread.Abort(); } catch { }

                listenerThread = null;
                listener = null;
                hosted = false;

                result = HostResult.UnhandledException;
                return false;
            }
        }
        #endregion
        #region Public Vars
        public bool isRuning { get { return this.hosted; } }
        public SharpRoom Room { get { return this.room; } }

        public SharpClient Self { get { return this.serverclient; } }
        #endregion
        #region Listening Clients
        void ListenClients()
        {
            while (true)
            {
                TcpClient client = null;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception e)
                {
                    Disconnect();
                }

                try
                {
                    HandleIncomingConnection(client);
                }
                catch
                {
                }

            }
        }

        void HandleIncomingConnection(TcpClient client)
        {
            Stopwatch timeout = Stopwatch.StartNew();


            LockableBool isdone = new LockableBool();
            byte[] output = TCPMessageHandler.Read(client);
            //LockableObject<byte[]> output = null;
            //Thread thread;

            //TCPMessageHandler.Read(client, isdone, output, out thread);

            //while (!isdone)
            //{
            //    Thread.Sleep(1);
            //    if (timeout.ElapsedMilliseconds > this.timeout)
            //    {
            //        try { thread.Abort(); thread = null; } catch { }
            //        TCPMessageHandler.CloseConnection(client);
            //        return;
            //    }
            //}
            SharpSerializer package = SharpSerializer.Create(output);
            try
            {
                HandleIncomingConnectionRequest(package, client);
            }
            catch (Exception e)
            {
                TCPMessageHandler.CloseConnection(client);
            }

            package.Post();
        }


        void HandleIncomingConnectionRequest(SharpSerializer incomingData, TcpClient client)
        {
            IncomingConnectionRequests request = (IncomingConnectionRequests)incomingData.ReadByte();
            if (request == IncomingConnectionRequests.RoomDetails)
            {
                //Sending Room Details And Goodby
                SharpRoomDetails room_details = this.room.GetAsDetails();

                SharpSerializer respond = SharpSerializer.Create();
                room_details.Write(ref respond);

                TCPMessageHandler.Write(client, respond.DataAndPost());

                TCPMessageHandler.CloseConnection(client);

                return;
            }
            else if (request == IncomingConnectionRequests.ConnectionApprove)
            {
                SharpSerializer respond = SharpSerializer.Create();

                SharpClient incomingClient = new SharpClient(ref incomingData);

                if (this.room.isFull)
                {
                    respond.Write((byte)IncomingConnectionResponse.ServerIsFull);
                    TCPMessageHandler.Write(client, respond.DataAndPost());
                    TCPMessageHandler.CloseConnection(client);
                    return;
                }

                if (this.room.isExistName(incomingClient.ClientName))
                {
                    respond.Write((byte)IncomingConnectionResponse.NameIsExist);
                    TCPMessageHandler.Write(client, respond.DataAndPost());
                    TCPMessageHandler.CloseConnection(client);
                    return;
                }

                string incomingPassword = incomingData.ReadString();
                if (!this.room.isValidPassword(incomingPassword))
                {
                    respond.Write((byte)IncomingConnectionResponse.WrongPassword);
                    TCPMessageHandler.Write(client, respond.DataAndPost());
                    TCPMessageHandler.CloseConnection(client);
                    return;
                }

                HandleAcceptedConnection(client, incomingClient);
            }
            else
                TCPMessageHandler.CloseConnection(client);
        }


        void HandleAcceptedConnection(TcpClient client, SharpClient sclient)
        {
            SharpSerializer syncPack = SharpSerializer.Create();
            syncPack.Write((byte)IncomingConnectionResponse.Approved);

            short netID = this.room.NextFreeID;
            sclient.AssingNetID(netID);


            syncPack.Write(netID);
            this.room.Write(ref syncPack);

            TCPMessageHandler.Write(client, syncPack.DataAndPost());

            this.room.AssignClient(sclient);
            Thread t = new Thread(() => ListenClient(sclient));

            sclient.ListeningThread = t;
            sclient.Connection = client;

            t.Start();

            S_OnClientConnected(sclient);
        }


        void ListenClient(SharpClient client)
        {
            while (true)
            {
                byte[] package = null;
                try
                {
                    package = TCPMessageHandler.Read(client.Connection);
                }
                catch
                {
                    S_OnClientDisconnected(client);
                    return;
                }

                S_OnReceivedMessage(client, package);
            }
        }

        #endregion
        #region Broadcast
        void Broadcast(byte[] data, SharpClient except)
        {
            foreach (var item in this.room.ConnectedClients)
            {
                if (item == except)
                    continue;
                item.SendMessage(data);
            }
        }

        #endregion
        #region Data Broadcasting
        public void SendMessage(int Channel, byte[] data, SharpTargets targets)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.DirectData);

            ser.Write((short)0);
            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);

            if (targets == SharpTargets.All)
            {
                Broadcast(ser.DataAndPost(), null);
                S_OnReceivedData(serverclient, Channel, data);
            }
            else if (targets == SharpTargets.ClientsOnly)
            {
                Broadcast(ser.DataAndPost(), null);
            }
            else if (targets == SharpTargets.OtherClientsOnly)
            {
                Broadcast(ser.DataAndPost(), null);
            }
            else if (targets == SharpTargets.Others)
            {
                Broadcast(ser.DataAndPost(), null);
            }
            else if (targets == SharpTargets.Server)
            {
                S_OnReceivedData(serverclient, Channel, data);
            }

            if (ser.inUse)
                ser.Post();
        }
        public void SendMessage(int Channel, byte[] data, SharpClient client)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.DirectData);

            ser.Write((short)0);
            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);

            if (client.isMine)
                S_OnReceivedData(serverclient, Channel, data);
            else
                client.SendMessage(ser.DataAndPost());

            if (ser.inUse)
                ser.Post();
        }
        public void SendMessage(int Channel, byte[] data, List<SharpClient> clients)
        {
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.DirectData);

            ser.Write((short)0);
            ser.Write(Channel);
            ser.Write(data.Length);
            ser.Write(data);


            byte[] rawData = ser.DataAndPost();
            foreach (var item in clients)
            {
                if (item.isMine)
                    S_OnReceivedData(serverclient, Channel, data);
                else
                    item.SendMessage(rawData);
            }
        }

        #endregion
        #region Disconnect / Reset
        public void Disconnect()
        {
            if (!hosted)
                throw new Exception("You are not hosted already");
            hosted = false;

            foreach (var item in this.room.ConnectedClients)
            {
                try
                {
                    TCPMessageHandler.CloseConnection(item.Connection);
                }
                catch { }

                try
                {
                    item.ListeningThread.Abort();
                }
                catch { }

                item.ListeningThread = null;
            }

            port = 0;
            serverclient = null;
            room = null;



            try
            {
                listener.Stop();
            }
            catch { }
            try
            {
                listener = null;
            }
            catch { }

            try
            {
                S_StoppedHosted();
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
        void S_OnHosted()
        {
            SharpNetworking.Instance._OnNetworkEvent(SharpNetworkEvent.Connected);
        }
        void S_StoppedHosted()
        {
            SharpNetworking.Instance._OnNetworkEvent(SharpNetworkEvent.Disconnected);
        }
        void S_OnClientConnected(SharpClient client)
        {
            #region Broadcasting To Other clients
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.ClientConnected);
            client.Write(ref ser);

            Broadcast(ser.DataAndPost(), client);
            #endregion

            SharpNetworking.Instance._OnClientConnected(client);
        }
        void S_OnClientDisconnected(SharpClient client)
        {

            #region Broadcasting Other Clients
            SharpSerializer ser = SharpSerializer.Create();
            ser.Write((byte)SharpConnectionMessages.ClientDisconnected);
            ser.Write(client.NetworkID);

            Broadcast(ser.DataAndPost(), client);
            #endregion

            SharpNetworking.Instance.OnClientDisconnected(client);

            room.RemoveClient(client);

            #region Closing Socket & Thread
            try
            {
                TCPMessageHandler.CloseConnection(client.Connection);
            }
            catch { }
            try
            {
                client.ListeningThread.Abort();
            }
            catch { }
            client.ListeningThread = null;

            #endregion
        }
        void S_OnReceivedMessage(SharpClient sender, byte[] data)
        {
            SharpSerializer ser = SharpSerializer.Create(data);
            SharpConnectionMessages type = (SharpConnectionMessages)ser.ReadByte();
            if (type == SharpConnectionMessages.MessageType1)
            {
                #region Message Type 1
                SharpTargets targets = (SharpTargets)ser.ReadByte();
                int channel = ser.ReadInt32();
                int size = ser.ReadInt32();
                byte[] rawData = ser.ReadBytes(size);


                if (targets == SharpTargets.Server)
                {
                    S_OnReceivedData(sender, channel, rawData);
                    return;
                }

                SharpSerializer package = SharpSerializer.Create();
                package.Write((byte)SharpConnectionMessages.DirectData);

                package.Write(sender.NetworkID);
                package.Write(channel);
                package.Write(size);
                package.Write(rawData);
                if (targets == SharpTargets.All)
                {
                    Broadcast(package.DataAndPost(), null);
                    S_OnReceivedData(sender, channel, rawData);
                }
                else if (targets == SharpTargets.ClientsOnly)
                {
                    Broadcast(package.DataAndPost(), null);
                }
                else if (targets == SharpTargets.OtherClientsOnly)
                {
                    Broadcast(package.DataAndPost(), sender);
                }
                else if (targets == SharpTargets.Others)
                {
                    Broadcast(package.DataAndPost(), sender);
                    S_OnReceivedData(sender, channel, rawData);
                }
                #endregion
            }
            else if (type == SharpConnectionMessages.MessageType2)
            {
                #region Message Type 2
                short targetID = ser.ReadInt16();
                int channel = ser.ReadInt32();
                int size = ser.ReadInt32();
                byte[] rawData = ser.ReadBytes(size);

                if (targetID == 0)
                {
                    S_OnReceivedData(sender, channel, rawData);
                    return;
                }

                SharpSerializer package = SharpSerializer.Create();
                package.Write((byte)SharpConnectionMessages.DirectData);
                package.Write(sender.NetworkID);
                package.Write(channel);
                package.Write(size);
                package.Write(rawData);

                this.room.Get(targetID).SendMessage(package.DataAndPost());
                #endregion
            }
            else if (type == SharpConnectionMessages.MessageType3)
            {
                #region Message Type 3
                short targetCount = ser.ReadInt16();
                short[] targets = new short[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    targets[i] = ser.ReadInt16();
                }

                int channel = ser.ReadInt32();
                int size = ser.ReadInt32();
                byte[] rawData = ser.ReadBytes(size);

                SharpSerializer package = SharpSerializer.Create();
                package.Write((byte)SharpConnectionMessages.DirectData);
                package.Write(sender.NetworkID);
                package.Write(channel);
                package.Write(size);
                package.Write(rawData);

                byte[] goData = package.DataAndPost();
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == 0)
                        S_OnReceivedData(sender, channel, rawData);
                    else
                        this.room.Get(targets[i]).SendMessage(goData);
                }
                #endregion
            }
        }


        void S_OnReceivedData(SharpClient sender, int channel, byte[] data)
        {
            SharpNetworking.Instance.OnMessageReceived(sender, channel, data);
        }

        #endregion

    }
}
