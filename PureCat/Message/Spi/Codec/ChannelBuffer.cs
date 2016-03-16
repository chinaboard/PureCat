using System.Text;
using System.IO;
using System;

namespace PureCat.Message.Spi.Codec
{
    public class ChannelBuffer : IDisposable
    {
        private readonly MemoryStream _memoryBuf;

        private readonly BinaryWriter _binaryWriter;

        public ChannelBuffer(int capacity)
        {
            _memoryBuf = new MemoryStream(capacity);
            _binaryWriter = new BinaryWriter(_memoryBuf, Encoding.UTF8);
        }

        /// <summary>
        ///   从当前位置到目标字符第一次出现的位置有多少字节?
        /// </summary>
        /// <param name="separator"> </param>
        /// <returns> </returns>
        public int BytesBefore(byte separator)
        {
            int count = 0;
            long oldPosition = _memoryBuf.Position;

            while (_memoryBuf.Position < _memoryBuf.Length)
            {
                int b = _memoryBuf.ReadByte();

                if (b == -1)
                {
                    return -1;
                }
                if ((byte)b == separator)
                {
                    _memoryBuf.Position = oldPosition;
                    return count;
                }

                count++;
            }

            _memoryBuf.Position = oldPosition;
            return 0;
        }

        public void Skip(int bytes)
        {
            _memoryBuf.Position += bytes;
        }

        public int ReadableBytes()
        {
            return (int)(_memoryBuf.Length - _memoryBuf.Position);
        }

        public int ReadBytes(byte[] data)
        {
            return _memoryBuf.Read(data, 0, data.Length);
        }

        public byte ReadByte()
        {
            return (byte)(_memoryBuf.ReadByte() & 0xFF);
        }

        public void WriteByte(byte b)
        {
            _binaryWriter.Write(b);
        }

        public void WriteByte(char c)
        {
            _binaryWriter.Write((byte)(c & 0xFF));
        }

        public void WriteInt(int i)
        {
            _binaryWriter.Write(ToBytes(i));
        }

        public void WriteBytes(byte[] data)
        {
            _binaryWriter.Write(data);
        }

        public void WriteBytes(byte[] data, int offset, int len)
        {
            _binaryWriter.Write(data, offset, len);
        }

        // for test purpose
        public void Reset()
        {
            _memoryBuf.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        ///   在流的相应位置插入一个整数的字节(覆盖？)
        /// </summary>
        /// <param name="index"> </param>
        /// <param name="i"> </param>
        public void SetInt(int index, int i)
        {
            _binaryWriter.Seek(index, SeekOrigin.Begin);
            _binaryWriter.Write(ToBytes(i));
        }

        private static byte[] ToBytes(int value)
        {
            byte[] bytes = new byte[4];

            bytes[3] = (byte)value;
            bytes[2] = (byte)(value >> 8);
            bytes[1] = (byte)(value >> 16);
            bytes[0] = (byte)(value >> 24);
            return bytes;
        }

        public byte[] ToArray()
        {
            return _memoryBuf.ToArray();
        }

        /// <summary>
        ///   从当前位置到结尾的字节数组的字符串表示
        /// </summary>
        /// <returns> </returns>
        public override string ToString()
        {
            byte[] data = _memoryBuf.ToArray();
            int offset = (int)_memoryBuf.Position;
            string str = Encoding.UTF8.GetString(data, offset, data.Length - offset);

            //ToArray本身就不为该Position，所以下一行代码多余
            //_mBuf.Seek(offset, SeekOrigin.Begin);
            return str;
        }

        public void Dispose()
        {
            _binaryWriter?.Close();
            _memoryBuf?.Close();

            GC.SuppressFinalize(this);
        }
    }
}