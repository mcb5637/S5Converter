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
            if (args.Length == 0)
            {
                try
                {
                    using BinaryReader r = new(Console.OpenStandardInput());
                    RWFile d = RWFile.Read(r);
                    using Stream ou = Console.OpenStandardOutput();
                    JsonSerializer.Serialize(ou, d, SourceGenerationContext.Default.RWFile);
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

                }
                else
                {
                    try
                    {
                        Console.Error.WriteLine($"converting {f}");
                        using BinaryReader r = new(new FileStream(f, FileMode.Open, FileAccess.Read));
                        RWFile d = RWFile.Read(r);
                        using FileStream ou = new(Path.ChangeExtension(f, ".json"), FileMode.Create, FileAccess.Write);
                        JsonSerializer.Serialize(ou, d, SourceGenerationContext.Default.RWFile);
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
            Console.Read();
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RWFile))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}