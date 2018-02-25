using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet.Socket
{
    enum SharpConnectionMessages : byte
    {
        ClientConnected,ClientDisconnected,MessageType1,MessageType2,MessageType3,DirectData,
        StartSendingFile
    }
}
