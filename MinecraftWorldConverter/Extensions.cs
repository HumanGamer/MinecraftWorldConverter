using System;
using System.IO;

namespace MinecraftWorldConverter
{
    public static class Extensions
    {
        public static bool VerifyNext(this BinaryReader br, ulong magic)
        {
            return br.ReadUInt64() == magic;
        }
        
        public static bool VerifyNext(this BinaryReader br, uint magic)
        {
            return br.ReadUInt32() == magic;
        }
        
        public static bool VerifyNext(this BinaryReader br, ushort magic)
        {
            return br.ReadUInt16() == magic;
        }
        
        public static bool VerifyNext(this BinaryReader br, byte magic)
        {
            return br.ReadByte() == magic;
        }
        
        public static bool VerifyNext(this BinaryReader br, byte[] magic)
        {
            return br.ReadBytes(magic.Length).Matches(magic);
        }

        public static bool Matches(this byte[] self, byte[] other)
        {
            if (self.Length != other.Length)
                return false;

            for (var i = 0; i < self.Length; i++)
            {
                if (self[i] != other[i])
                    return false;
            }
            
            return true;
        }
    }
}