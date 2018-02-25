using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNet.Socket
{
    enum IncomingConnectionResponse : byte
    {
        Approved,ServerIsFull,NameIsExist,
        WrongPassword
    }
}
