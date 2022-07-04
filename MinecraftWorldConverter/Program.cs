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

            string outputFile;
            if (args.Length > 1)
                outputFile = args[1];
            else
                outputFile = Path.ChangeExtension(inputFile, null);//, "mclevel");
            
            ClassicWorld classicWorld = new ClassicWorld();
            //try
            //{
                classicWorld.LoadFromFile(inputFile);
            /*} catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Failed to read input file: " + Path.GetFileName(inputFile));
                Environment.Exit(1);
                return;
            }*/

            foreach (var property in classicWorld.GetPropertyMap())
            {
                Console.WriteLine(property.Key + " = " + property.Value + ";");
            }
            
            //Console.WriteLine("Saving Indev World(" + Path.GetFileName(outputFile) + ")...");
            //classicWorld.SaveIndevWorld(outputFile);
            
            //Console.WriteLine("Saving Alpha World(" + Path.GetFileName(outputFile) + ")...");
            //classicWorld.SaveAlphaWorld(outputFile);
            
            Console.WriteLine("Saving McRegion World(" + Path.GetFileName(outputFile) + ")...");
            classicWorld.SaveMcRegionWorld(outputFile);

            Console.WriteLine("Done!");
            
            

            //string outputFile = Path.ChangeExtension(args[0], "") + "_output";
        }
    }
}