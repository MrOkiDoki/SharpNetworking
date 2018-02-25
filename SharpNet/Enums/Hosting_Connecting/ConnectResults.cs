using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet.Socket
{
    enum ConnectResults
    {
        Succes, InvalidPassword, ServerIsFull,AlreadyConnected, UnhandledException,
        InvalidIpAddress,
        NameIsExist
    }
}
