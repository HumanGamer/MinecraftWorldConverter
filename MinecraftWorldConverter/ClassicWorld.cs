using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using fNbt;

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
        
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public float PlayerZ { get; set; }
        public float PlayerYaw { get; set; }
        public float PlayerPitch { get; set; }
        public float PlayerMotionX { get; set; }
        public float PlayerMotionY { get; set; }
        public float PlayerMotionZ { get; set; }
        
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
            
            // Player Stuff
            br.BaseStream.Position = 0x738;
            bool slide = br.ReadBoolean();
            int textureId = br.ReadInt32();
            float walkDist = br.ReadSingle();
            float walkDistO = br.ReadSingle();
            PlayerX = br.ReadSingle();
            float xOld = br.ReadSingle();
            PlayerPitch = br.ReadSingle();
            float xRotO = br.ReadSingle();
            PlayerMotionX = br.ReadSingle();
            float xo = br.ReadSingle();
            PlayerY = br.ReadSingle();
            float yOld = br.ReadSingle();
            PlayerYaw = br.ReadSingle();
            float yRotO = br.ReadSingle();
            float ySlideOffset = br.ReadSingle();
            PlayerMotionY = br.ReadSingle();
            float yo = br.ReadSingle();
            PlayerZ = br.ReadSingle();
            float zOld = br.ReadSingle();
            PlayerMotionZ = br.ReadSingle();
            float zo = br.ReadSingle();

            // Start of block array
            br.BaseStream.Position = 0x5096;
            BlockMapData = br.ReadBytes(0x400000);

            br.BaseStream.Position++;
            Creator = br.ReadJavaString();
            
            br.BaseStream.Position++;
            Name = br.ReadJavaString();
        }

        public void SaveIndevWorld(string fileName)
        {
            NbtFile file = new NbtFile();

            var lvl = new NbtCompound("MinecraftLevel");
            {
                var about = new NbtCompound("About");
                {
                    about.Add(new NbtLong("CreatedOn", CreateTime));
                    about.Add(new NbtString("Name", Name));
                    about.Add(new NbtString("Author", Creator));
                }
                lvl.Add(about);
                
                var environment = new NbtCompound("Environment");
                {
                    //environment.Add(new NbtShort("TimeOfDay", 0));
                    environment.Add(new NbtByte("SkyBrightness", 100));
                    environment.Add(new NbtInt("SkyColor", SkyColor));
                    environment.Add(new NbtInt("FogColor", FogColor));
                    environment.Add(new NbtInt("CloudColor", CloudColor));
                    environment.Add(new NbtShort("CloudHeight", 66));
                    environment.Add(new NbtByte("SurroundingGroundType", 2));
                    environment.Add(new NbtShort("SurroundingGroundHeight", 23));
                    environment.Add(new NbtByte("SurroundingWaterType", 8));
                    environment.Add(new NbtShort("SurroundingWaterHeight", 32));
                }
                lvl.Add(environment);
                
                var map = new NbtCompound("Map");
                {
                    map.AddList("Spawn", new []{(short)SpawnX, (short)SpawnY, (short)SpawnZ});
                    map.Add(new NbtShort("Height", (short)Depth));
                    map.Add(new NbtShort("Length", (short)Height));
                    map.Add(new NbtShort("Width", (short)Width));
                    map.Add(new NbtByteArray("Blocks", BlockMapData));

                    int length = Width * Height * Depth;
                    byte[] data = new byte[length];
                    Array.Fill<byte>(data, 15);
                    map.Add(new NbtByteArray("Data", data));
                }
                lvl.Add(map);
                
                var entities = new NbtList("Entities", NbtTagType.Compound);
                {
                    // TODO: Deal with entities other than the player (and all their properties)
                    
                    NbtCompound player = new NbtCompound();
                    
                    player.Add(new NbtString("id", "LocalPlayer"));
                    player.AddList("Pos", new []{ PlayerX, PlayerY, PlayerZ });
                    player.AddList("Rotation", new []{ PlayerYaw, PlayerPitch });
                    player.AddList("Motion", new []{ PlayerMotionX, PlayerMotionY, PlayerMotionZ });
                    player.Add(new NbtFloat("FallDistance", 0));
                    player.Add(new NbtShort("Health", 20));
                    player.Add(new NbtShort("AttackTime", 0));
                    player.Add(new NbtShort("HurtTime", 0));
                    player.Add(new NbtShort("DeathTime", 0));
                    player.Add(new NbtShort("Air", 300));
                    player.Add(new NbtShort("Fire", 0));
                    player.Add(new NbtInt("Score", 0));
                    player.Add(new NbtList("Inventory", NbtTagType.Compound));
                    
                    entities.Add(player);
                }
                lvl.Add(entities);
                
                var tileEntities = new NbtList("TileEntities", NbtTagType.Compound);
                {
                    // No Tile Entities in Classic Worlds
                }
                lvl.Add(tileEntities);
            }
            file.RootTag = lvl;

            file.SaveToFile(fileName, NbtCompression.GZip);
        }
    }
}