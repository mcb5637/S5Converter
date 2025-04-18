using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Programm
    {
        internal static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--import")
            {
                try
                {
                    using BinaryReader r = new(Console.OpenStandardInput());
                    using Stream ou = Console.OpenStandardOutput();
                    Import(r, ou);
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine(e.ToString());
                }
                return;
            }
            else if (args.Length == 1 && args[0] == "--export")
            {
                try
                {
                    using Stream r = Console.OpenStandardInput();
                    using BinaryWriter ou = new(Console.OpenStandardOutput());
                    Export(r, ou);
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine(e.ToString());
                }
                return;
            }
            foreach (string f in args)
            {
                if (f.EndsWith(".json"))
                {
                    try
                    {
                        Console.Error.WriteLine($"converting {f}");
                        using FileStream r = new(f, FileMode.Open, FileAccess.Read);
                        using BinaryWriter ou = new(new FileStream(Path.ChangeExtension(f, ".out"), FileMode.Create, FileAccess.Write));
                        Export(r, ou);
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
                else
                {
                    try
                    {
                        Console.Error.WriteLine($"converting {f}");
                        using BinaryReader r = new(new FileStream(f, FileMode.Open, FileAccess.Read));
                        using FileStream ou = new(Path.ChangeExtension(f, ".json"), FileMode.Create, FileAccess.Write);
                        Import(r, ou);
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
            Console.Read();
        }

        private static void Import(BinaryReader r, Stream ou)
        {
            RWFile d = RWFile.Read(r);
            JsonSerializer.Serialize(ou, d, SourceGenerationContext.Default.RWFile);
        }

        private static void Export(Stream r, BinaryWriter ou)
        {
            RWFile d = JsonSerializer.Deserialize(r, SourceGenerationContext.Default.RWFile) ?? throw new IOException("failed to parse file");
            d.Write(ou);
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RWFile))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}