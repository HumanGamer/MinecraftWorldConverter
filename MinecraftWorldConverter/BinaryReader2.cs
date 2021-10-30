using System;
using System.IO;
using System.Text;

namespace MinecraftWorldConverter
{
    public class BinaryReader2 : BinaryReader
    {
        public BinaryReader2(Stream input) : base(input)
        {
        }

        public BinaryReader2(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BinaryReader2(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public byte PeekByte()
        {
            var pos = BaseStream.Position;
            
            var ret = ReadByte();
            
            BaseStream.Position = pos;
            return ret;
        }

        public override int ReadInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override short ReadInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override long ReadInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override uint ReadUInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override ushort ReadUInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override ulong ReadUInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override float ReadSingle()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public override double ReadDouble()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }

        public string ReadJavaString()
        {
            var len = ReadUInt16();
            return Encoding.ASCII.GetString(ReadBytes(len));
        }
    }
}