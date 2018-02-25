using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNet
{
    class SharpSerializer
    {
        #region Pooling
        private static Queue<SharpSerializer> pool = new Queue<SharpSerializer>();

        private bool _inUse = false;
        #endregion
        #region Buffer
        byte[] buffer = new byte[4];
        int write_position = 0;
        int read_posititon = 0;
        #endregion
        #region Functions
        void CheckBuffer(int Size)
        {
            Check();
            if (buffer.Length <= write_position + Size)
                Array.Resize(ref buffer, write_position + Size + 256);
        }

        #endregion
        #region Main
        private SharpSerializer(byte[] Data)
        {
            buffer = Data;
            this._inUse = true;
        }
        private SharpSerializer()
        {
            this._inUse = true;
        }
        private void Load(byte[] Data)
        {
            this.buffer = Data;
            this._inUse = true;
        }
        #endregion
        #region Public Vars
        public bool inUse { get { return this._inUse; } }
        public int ReadPosition { get { return this.read_posititon; } set { this.read_posititon = value; } }
        public int WritePosition { get { return this.write_position; } set { this.write_position = value; } }
        public int BufferSize { get { return this.buffer.Length; } }

        #endregion

        #region Write
        public void Write(byte value)
        {
            CheckBuffer(1);
            buffer[write_position] = value;
            write_position++;
        }
        public void Write(bool value)
        {
            CheckBuffer(1);
            if (value)
                Write((byte)1);
            else
                Write((byte)0);
        }
        public void Write(byte[] value)
        {
            CheckBuffer(value.Length);
            System.Buffer.BlockCopy(value, 0, buffer, write_position, value.Length);
            write_position += value.Length;
        }
        public unsafe void Write(short value)
        {
            CheckBuffer(2);
            fixed (byte* Adress = &buffer[write_position])//0x5
            {
                write_position += 2;

                short* s = (short*)Adress;
                *s = value;
            }
        }
        public unsafe void Write(int value)
        {
            CheckBuffer(4);
            fixed (byte* Adress = &buffer[write_position])
            {
                write_position += 4;

                int* s = (int*)Adress;
                *s = value;
            }
        }
        public unsafe void Write(double value)
        {
            CheckBuffer(8);
            fixed (byte* Adress = &buffer[write_position])
            {
                write_position += 8;

                double* s = (double*)Adress;
                *s = value;
            }
        }
        public unsafe void Write(float value)
        {
            CheckBuffer(4);
            fixed (byte* Adress = &buffer[write_position])
            {
                write_position += 4;

                float* s = (float*)Adress;
                *s = value;
            }
        }
        public void Write(string text)
        {

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            Write((short)bytes.Length);

            CheckBuffer(bytes.Length);
            Write(bytes);
        }
        #endregion
        #region Read
        public byte ReadByte()
        {
            Check();
            return buffer[read_posititon++];
        }
        public bool ReadBool()
        {
            Check();
            return (ReadByte() == 1);
        }
        public byte[] ReadBytes(int Size)
        {
            Check();
            byte[] tempBuffer = new byte[Size];
            System.Buffer.BlockCopy(buffer, read_posititon, tempBuffer, 0, Size);
            read_posititon += Size;
            return tempBuffer;
        }
        public unsafe short ReadInt16()
        {
            Check();
            fixed (byte* Adress = &buffer[read_posititon])
            {
                read_posititon += 2;
                return *((short*)Adress);
            }
        }
        public unsafe int ReadInt32()
        {
            Check();
            fixed (byte* Adress = &buffer[read_posititon])
            {
                read_posititon += 4;
                return *((int*)Adress);
            }
        }
        public unsafe long ReadInt64()
        {
            Check();
            fixed (byte* Adress = &buffer[read_posititon])
            {
                read_posititon += 8;
                return *((long*)Adress);
            }
        }
        public string ReadString()
        {
            Check();
            short size = ReadShort();
            byte[] text = ReadBytes(size);
            return Encoding.UTF8.GetString(text);
        }



        public short ReadShort()
        {
            return ReadInt16();
        }
        public int ReadInt()
        {
            return ReadInt32();
        }
        public long ReadLong()
        {
            return ReadInt64();
        }


        #endregion
        #region Create Post & Check
        void Check()
        {
            if (!inUse)
                throw new Exception("Serializer is not in use!");
        }


        public static SharpSerializer Create(byte[] data)
        {
            if (pool.Count > 0)
            {
                SharpSerializer obj = pool.Dequeue();
                if (obj != null)
                {
                    obj.Load(data);
                    return obj;
                }
                return new SharpSerializer(data);
            }
            return new SharpSerializer(data);
        }
        public static SharpSerializer Create()
        {
            if (pool.Count > 0)
            {
                SharpSerializer obj = pool.Dequeue();
                if (obj != null)
                {
                    obj._inUse = true;
                    return obj;
                }
                return new SharpSerializer();
            }
            return new SharpSerializer();
        }

        public void Post()
        {
            this._inUse = false;

            read_posititon = 0;
            write_position = 0;

            pool.Enqueue(this);
        }

        #endregion
        #region Output
        public byte[] Data()
        {
            if (buffer.Length == write_position)
                return buffer;
            byte[] output = new byte[write_position];
            System.Buffer.BlockCopy(buffer, 0, output, 0, write_position);
            return output;
        }
        public byte[] DataAndPost()
        {
            byte[] go = Data();
            Post();
            return go;
        }

        #endregion
    }
}
