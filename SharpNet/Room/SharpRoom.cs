using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNet
{
    class SharpRoom
    {
        #region Variables
        List<SharpClient> connections = new List<SharpClient>();
        SharpClient[] slots = new SharpClient[0];
        string room_name;
        string room_password;
        short max_connection;
        short clientCount = 0;
        #endregion
        #region Main
        public SharpRoom(string RoomName, short MaxConnection)
        {
            this.room_name = RoomName;
            this.room_password = "";
            this.max_connection = MaxConnection;
            this.slots = new SharpClient[MaxConnection];
        }
        public SharpRoom(string RoomName, string Password, short MaxConnection)
        {
            if (Password == null)
                Password = "";

            this.room_name = RoomName;
            this.room_password = Password;
            this.max_connection = MaxConnection;
            this.slots = new SharpClient[MaxConnection];
        }
        public SharpRoom(ref SharpSerializer ser)
        {
            Read(ref ser);
        }
        #endregion
        #region Write/Read
        public void Write(ref SharpSerializer ser)
        {
            ser.Write(this.room_name);
            ser.Write(this.room_password);
            ser.Write(this.max_connection);
            for (int i = 0; i < slots.Length; i++)
            {
                SharpClient c = slots[i];
                bool isExist = (c != null);
                if (!isExist)
                    ser.Write(false);
                else
                {
                    ser.Write(true);
                    c.Write(ref ser);
                }
            }
        }
        private void Read(ref SharpSerializer ser)
        {
            this.room_name = ser.ReadString();
            this.room_password = ser.ReadString();
            this.max_connection = ser.ReadInt16();
            this.slots = new SharpClient[this.max_connection];
            for (int i = 0; i < slots.Length; i++)
            {
                bool isExist = ser.ReadBool();
                if (isExist)
                {
                    slots[i] = new SharpClient(ref ser);
                    slots[i].SetIsConnected(true);
                    if (i != 0)
                        connections.Add(slots[i]);
                }
                else if (this.clientCount == 0)
                    this.clientCount = (short)i;
            }
        }

        #endregion
        #region Public Vars
        public string RoomName { get { return this.room_name; } }
        public string RoomPasswod { get { return this.room_password; } }
        public short ClientCount { get { return this.clientCount; } }
        public short MaxConnectionCount { get { return this.max_connection; } }
        public short NextFreeID
        {
            get
            {
                for (int i = 0; i < this.slots.Length; i++)
                {
                    if (this.slots[i] != null)
                        continue;
                    return (short)i;
                }
                throw new Exception("Max Player Limit (65535)");
            }
        }
        public bool isFull
        {
            get
            {
                return this.clientCount == max_connection;
            }
        }
        public List<SharpClient> ConnectedClients { get { return this.connections; } }

        public bool isSecured { get { return this.room_password != ""; } }
        #endregion
        #region Functions
        public bool isValidPassword(string Password)
        {
            if (this.room_password == string.Empty)
                return true;
            return this.room_password == Password;
        }
        public bool isExistName(string name)
        {
            for (int i = 0; i < this.slots.Length; i++)
            {
                if (this.slots[i] != null)
                {
                    if (this.slots[i].ClientName == name)
                        return true;
                }
                else
                    return false;
            }
            return false;
        }

        public void AssignClient(SharpClient client)
        {
            this.slots[client.NetworkID] = client;
            client.SetIsConnected(true);
            clientCount++;
            if (client.NetworkID != 0)
                connections.Add(client);
        }
        public void RemoveClient(SharpClient client)
        {
            this.slots[client.NetworkID] = null;
            client.SetIsConnected(false);
            clientCount--;
            connections.Remove(client);
        }

        public SharpClient Get(short NetID)
        {
            return slots[NetID];
        }
        public SharpClient Server
        {
            get
            {
                return this.slots[0];
            }
        }

        public SharpRoomDetails GetAsDetails()
        {
            return new SharpRoomDetails(this);
        }
        #endregion
    }
}
