using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

enum SharpTargets:byte
{
    /// <summary>
    /// Server Broadcast everyone(Including you) and Server Execute it too
    /// </summary>
    All,
    /// <summary>
    /// Server Broadcast everyone(Except you) And Server Execute it too
    /// </summary>
    Others,
    /// <summary>
    /// Only Server Receives and the Only Server Execute it
    /// </summary>
    Server,
    /// <summary>
    /// Server Broadcast everyone(Including you) But Server Doesn't Execute it
    /// </summary>
    ClientsOnly,
    /// <summary>
    /// Server Broadcast everyone(Except you) and Server doesn't execute it
    /// </summary>
    OtherClientsOnly
}