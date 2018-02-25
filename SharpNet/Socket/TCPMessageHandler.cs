using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpNet.Socket
{
    class TCPMessageHandler
    {
        public static void Write(TcpClient client, byte[] data)
        {
            if (data.Length == 0)
                throw new Exception("Data Size can not be 0");
            NetworkStream stream = client.GetStream();

            int size = data.Length;
            byte[] size_in_bytes = BitConverter.GetBytes(size);

            byte[] finalBytes = new byte[data.Length + 4];
            System.Buffer.BlockCopy(data, 0, finalBytes, 4, data.Length);
            System.Buffer.BlockCopy(size_in_bytes, 0, finalBytes, 0, 4);

            stream.Write(finalBytes, 0, finalBytes.Length);

        }

        public static byte[] Read(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] size_in_bytes = new byte[4];
            stream.Read(size_in_bytes, 0, 4);


            int MainPackageSize = BitConverter.ToInt32(size_in_bytes, 0);
            if (MainPackageSize == 0)
                throw new EndOfStreamException();

            int ReadedSize = 0; // 0
            int LeftToReadSize = MainPackageSize; // 30

            byte[] finalBuffer = null;
            byte[] Tempbuffer = new byte[MainPackageSize]; //30

            while (LeftToReadSize > 0) // 30 | 10
            {
                int incomingSpilitedPackageSize = stream.Read(Tempbuffer, 0, LeftToReadSize); // 20 | 10

                if (incomingSpilitedPackageSize == MainPackageSize)
                    return Tempbuffer;

                if (finalBuffer == null)
                    finalBuffer = new byte[MainPackageSize]; //30

                Buffer.BlockCopy(Tempbuffer, 0, finalBuffer, ReadedSize, incomingSpilitedPackageSize); // 20 Moved


                ReadedSize += incomingSpilitedPackageSize;//0-> 20
                LeftToReadSize = MainPackageSize - ReadedSize; // 30-20 => 10
            }

            return finalBuffer;
        }


        public static void ReadAsync(TcpClient client, LockableBool isDone, LockableObject<byte[]> output, out Thread ReadThread)
        {
            if (isDone == null)
                isDone = new LockableBool();
            if (output == null)
                output = new LockableObject<byte[]>();

            ReadThread = new Thread(() => _ReadAsnyc(client, output, isDone));
            ReadThread.Start();
        }
        private static void _ReadAsnyc(TcpClient client, LockableObject<byte[]> output, LockableBool isDone)
        {
            byte[] go = Read(client);
            output.Value = go;
            isDone.Value = true;
        }

        public static void CloseConnection(TcpClient client)
        {
            try
            {
                client.GetStream().Close();
            }
            catch { }
            try
            {
                client.Close();
            }
            catch
            {

            }
            client = null;
        }
    }
}
