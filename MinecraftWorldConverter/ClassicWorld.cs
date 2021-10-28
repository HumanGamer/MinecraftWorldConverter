using System;
using System.IO;
using System.IO.Compression;

namespace MinecraftWorldConverter
{
    public class ClassicWorld
    {
        private const uint MAGIC = 0x271bb788;
        
        private uint _magic;
        private byte _version;

        public int CloudColor { get; set; }
        public long CreateTime { get; set; }
        public bool CreativeMode { get; set; }
        public int Depth { get; set; }
        public int FogColor { get; set; }
        public bool GrowTrees { get; set; }
        public int Height { get; set; }
        public bool NetworkMode { get; set; }
        public float RotSpawn { get; set; }
        public int SkyColor { get; set; }
        public int WaterLevel { get; set; }
        public int Width { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public int SpawnZ { get; set; }
        
        public string Creator { get; set; }
        public string Name { get; set; }
        
        public int BlockMapDepth { get; set; }
        public int BlockMapHeight { get; set; }
        public int BlockMapWidth { get; set; }
        
        public byte[] BlockMapData { get; set; }
        
        public ClassicWorld()
        {
            
        }

        public void LoadFromFile(string file)
        {
            LoadFromStream(File.OpenRead(file));
        }

        public void LoadFromStream(Stream s)
        {
            // Classic worlds are GZip compressed
            using GZipStream stream = new GZipStream(s, CompressionMode.Decompress, false);
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            stream.Flush();
            stream.Close();

            ms.Position = 0;
            using BinaryReader2 br = new BinaryReader2(ms);

            _magic = br.ReadUInt32();
            if (_magic != MAGIC)
                throw new MCWorldException("Invalid Classic World");
            
            _version = br.ReadByte();
            
            // Start of Level Class section
            br.BaseStream.Position = 0x18E;
            CloudColor = br.ReadInt32();
            CreateTime = br.ReadInt64();
            CreativeMode = br.ReadBoolean();
            Depth = br.ReadInt32();
            FogColor = br.ReadInt32();
            GrowTrees = br.ReadBoolean();
            Height = br.ReadInt32();
            NetworkMode = br.ReadBoolean();
            RotSpawn = br.ReadSingle();
            SkyColor = br.ReadInt32();
            br.ReadInt32(); // tick count, why was this stored???
            br.ReadInt32(); // unprocessed ticks, why was this stored???
            WaterLevel = br.ReadInt32();
            Width = br.ReadInt32();
            SpawnX = br.ReadInt32();
            SpawnY = br.ReadInt32();
            SpawnZ = br.ReadInt32();
            
            // Start of Block Map section
            br.BaseStream.Position = 0x2A0;
            BlockMapDepth = br.ReadInt32();
            BlockMapHeight = br.ReadInt32();
            BlockMapWidth = br.ReadInt32();

            // Start of block array
            br.BaseStream.Position = 0x5096;
            BlockMapData = br.ReadBytes(0x400000);

            br.BaseStream.Position++;
            Creator = br.ReadJavaString();
            
            br.BaseStream.Position++;
            Name = br.ReadJavaString();
        }
    }
}