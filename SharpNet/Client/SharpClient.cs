using SharpNet.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet
{
    class SharpClient
    {
        #region TCP Socket
        public System.Threading.Thread ListeningThread;
        public System.Net.Sockets.TcpClient Connection;
        #endregion

        #region Create
        public SharpClient(string Name)
        {
            this.Name = Name;
            this.isme = true;
        }
        public SharpClient(ref SharpSerializer ser)
        {
            Read(ref ser);
        }

        #endregion
        #region Variables
        string Name;
        short netID;
        bool isme = false;
        bool isconnected = false;
        #endregion
        #region Public Read
        public string ClientName { get { return this.Name; } }
        public short NetworkID { get { return this.netID; } }
        public bool isHost { get { return this.netID == 0; } }
        public bool isMine { get { return this.isme; } }
        public bool isConnected { get { return this.isconnected; } }
        #endregion
        #region Read&Write
        public void Write(ref SharpSerializer ser)
        {
            ser.Write(this.Name);
            ser.Write(this.netID);
        }
        private void Read(ref SharpSerializer ser)
        {
            this.Name = ser.ReadString();
            this.netID = ser.ReadInt16();
        }
        #endregion
        #region Functions
        public void SetIsConnected(bool isConnected)
        {
            this.isconnected = isConnected;
        }
        public void AssingNetID(short NetID)
        {
            this.netID = NetID;
        }
        #endregion
        #region Send Message
        public void SendMessage(byte[] data)
        {
            TCPMessageHandler.Write(this.Connection, data);
        }

        #endregion
    }
}
