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
            foreach (string f in args)
            {
                if (f.EndsWith(".json"))
                {

                }
                else
                {
                    try
                    {
                        Console.WriteLine($"converting {f}");
                        using BinaryReader r = new(new FileStream(f, FileMode.Open, FileAccess.Read));
                        ChunkHeader h = ChunkHeader.FindChunk(r, RwCorePluginID.CLUMP);
                        Clump c = Clump.Read(r);
                        using FileStream ou = new(Path.ChangeExtension(f, ".json"), FileMode.Create, FileAccess.Write);
                        JsonSerializer.Serialize(ou, c, SourceGenerationContext.Default.Clump);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            Console.Read();
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Clump))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}