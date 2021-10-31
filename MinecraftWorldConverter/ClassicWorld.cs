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

        public void SaveAlphaWorld(string fileName)
        {
            if (!Directory.Exists(fileName))
                Directory.CreateDirectory(fileName);
            
            NbtFile levelDat = new NbtFile();

            var offset = -128;
            var yOffset = 32;

            var data = new NbtCompound("Data");
            {
                data.Add(new NbtLong("LastPlayed", CreateTime));
                data.Add(new NbtLong("SizeOnDisk", 0)); // TODO: Size on disk
                data.Add(new NbtLong("RandomSeed", new Random().Next()));
                data.Add(new NbtInt("SpawnX", SpawnX + offset));
                data.Add(new NbtInt("SpawnY", SpawnY + yOffset));
                data.Add(new NbtInt("SpawnZ", SpawnZ + offset));
                data.Add(new NbtLong("Time", 0));

                var player = new NbtCompound("Player");
                {
                    player.Add(new NbtInt("Dimension", 0));
                    player.AddList("Pos", new double[]{ PlayerX + offset + 0.5f, PlayerY + yOffset + 0.5f, PlayerZ + offset + 0.5f });
                    player.AddList("Rotation", new []{ PlayerYaw, PlayerPitch });
                    player.AddList("Motion", new double[]{ PlayerMotionX, PlayerMotionY, PlayerMotionZ });
                    player.Add(new NbtByte("OnGround", 1));
                    player.Add(new NbtFloat("FallDistance", 0));
                    player.Add(new NbtShort("Health", 20));
                    player.Add(new NbtShort("AttackTime", 0));
                    player.Add(new NbtShort("HurtTime", 0));
                    player.Add(new NbtShort("DeathTime", 0));
                    player.Add(new NbtShort("Air", 300));
                    player.Add(new NbtShort("Fire", 0));
                    player.Add(new NbtInt("Score", 0));
                    player.Add(new NbtList("Inventory", NbtTagType.Compound));
                }
                data.Add(player);
            }
            levelDat.RootTag.Add(data);

            levelDat.SaveToFile(Path.Combine(fileName, "level.dat"), NbtCompression.GZip);

            var blocks = new byte[32768 * 256];
            int ii = 0;
            for (int a = 0; a < 16; a++)
            {
                for (int b = 0; b < 16; b++)
                {
                    for (int z = a * 16; z < a * 16 + 16; z++)
                    {
                        for (int x = b * 16; x < b * 16 + 16; x++)
                        {
                            for (int j = 0; j < 32; j++)
                            {
                                blocks[ii + j] = 1; // 0 for floating world types if we ever support them
                            }

                            ii += 32;

                            for (int y = 0; y < 64; y++)
                            {
                                blocks[ii] = BlockMapData[(y * 256 + x) * 256 + z];
                                ii++;
                            }

                            for (int j = 0; j < 32; j++)
                            {
                                blocks[ii + j] = 0;
                            }

                            ii += 32;
                        }
                    }
                }
            }

            string[] base36List = new string[]{"1k", "1l", "1m", "1n", "1o", "1p", "1q", "1r", "0", "1", "2", "3", "4", "5", "6", "7"};
            int[] base36SignedList = new int[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7 };

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    int n = 16 * i + j;

                    NbtFile chunkFile = new NbtFile();
                    
                    // TODO: Deal with this later
                    var blockData = new byte[16384];
                    var blockLight = new byte[16384];
                    var skyLight = new byte[16384];
                    var heightMap = new byte[256];

                    var level = new NbtCompound("Level");
                    {
                        level.Add(new NbtInt("xPos", base36SignedList[i]));
                        level.Add(new NbtInt("zPos", base36SignedList[j]));
                        level.Add(new NbtByte("TerrainPopulated", 1));
                        level.Add(new NbtLong("LastUpdate", 200));
                        level.Add(new NbtByteArray("Blocks", blocks.Split(n * 32768, n * 32768 + 32768)));
                        level.Add(new NbtByteArray("Data", blockData));
                        level.Add(new NbtByteArray("BlockLight", blockLight));
                        level.Add(new NbtByteArray("SkyLight", skyLight));
                        level.Add(new NbtByteArray("HeightMap", heightMap));
                        level.Add(new NbtList("Entities", NbtTagType.Compound)); // TODO: Deal with this later
                        level.Add(new NbtList("TileEntities", NbtTagType.Compound)); // TODO: Deal with this later
                    }
                    chunkFile.RootTag.Add(level);

                    string path = Path.Combine(fileName, base36List[i], base36List[j]);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    chunkFile.SaveToFile(Path.Combine(path,
                        "c." + base36SignedList[i] + "." + base36SignedList[j] + ".dat"), NbtCompression.GZip);

                }
            }
        }
    }
}