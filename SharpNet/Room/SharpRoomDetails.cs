using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet
{
    class SharpRoomDetails
    {
        #region Variables
        private string roomName;
        private short maxConnection;
        private short currentConnnections;
        private short ping;
        private bool isSecured = false;
        private IPEndPoint connection;

        #endregion
        #region Public Vars
        public string RoomName { get { return this.roomName; } }
        public short CurrentConnectionsCount { get { return this.currentConnnections; } }
        public short MaxConnectionsCount { get { return this.maxConnection; } }
        public bool IsSecured { get { return this.isSecured; } }
        public short Ping { get { return this.ping; } }
        public IPEndPoint Connection { get { return this.connection; } }

        #endregion
        #region Main
        public SharpRoomDetails(SharpRoom room)
        {
            this.roomName = room.RoomName;
            this.maxConnection = room.MaxConnectionCount;
            this.currentConnnections = room.ClientCount;
            this.isSecured = room.isSecured;
        }
        public SharpRoomDetails(ref SharpSerializer ser)
        {
            Read(ref ser);
        }
        #endregion
        #region Functions
        public void Setup(short ping, IPEndPoint connection)
        {
            this.ping = ping;
            this.connection = connection;
        }

        #endregion

        #region Write/Read
        public void Write(ref SharpSerializer ser)
        {
            ser.Write(this.roomName);
            ser.Write(this.currentConnnections);
            ser.Write(this.maxConnection);
            ser.Write(this.isSecured);
        }
        private void Read(ref SharpSerializer ser)
        {
            this.roomName = ser.ReadString();
            this.currentConnnections = ser.ReadInt16();
            this.maxConnection = ser.ReadInt16();
            this.isSecured = ser.ReadBool();
        }


        #endregion
    }
}
