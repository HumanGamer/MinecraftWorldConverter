using System;
using System.IO;

namespace MinecraftWorldConverter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Minecraft World Converter ===");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MinecraftWorldConverter <world>");
                Environment.Exit(1);
                return;
            }
            
            string inputFile = args[0];
            WorldFile worldFile = new WorldFile();
            try
            {
                worldFile.LoadFromFile(inputFile);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Failed to read input file: " + Path.GetFileName(inputFile));
                Environment.Exit(1);
                return;
            }
            
            Console.WriteLine("Done!");
            
            

            //string outputFile = Path.ChangeExtension(args[0], "") + "_output";
        }
    }
}