using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Programm
    {
        internal static void Main(string[] args)
        {
            JsonSerializerOptions opt = new(JsonSerializerDefaults.General)
            {
                TypeInfoResolver = SourceGenerationContext.Default,
                WriteIndented = true,
            };
            if (args.Length >= 1 && args[0] == "--encodeFloatAsInt")
            {
                opt.Converters.Add(new EncodedFloatConverter());
                args = args[1..];
            }

            if (args.Length == 1 && args[0] == "--import")
            {
                try
                {
                    using BinaryReader r = new(Console.OpenStandardInput());
                    using Stream ou = Console.OpenStandardOutput();
                    Import(r, ou, opt);
                }
                catch (Exception e) when (e is IOException || e is JsonException)
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
                    Export(r, ou, opt);
                }
                catch (Exception e) when (e is IOException || e is JsonException)
                {
                    Console.Error.WriteLine(e.ToString());
                }
                return;
            }
            else if (args.Length == 2 && args[0] == "--searchParticles")
            {
                SearchParticles(args[1]);
                return;
            }
#if DEBUG
            else if (args.Length == 1 && args[0] == "--buildSchema")
            {
                string d = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(RWFile)).ToString();
                File.WriteAllText("./schema.json", d);
                return;
            }
            else if (args.Length == 2 && args[0] == "--checkRoundTrip")
            {
                CheckRoundTrip(args[1], opt);
                return;
            }
#endif
            foreach (string f in args)
            {
                if (f.EndsWith(".json"))
                {
                    try
                    {
                        Console.Error.WriteLine($"converting {f}");
                        using FileStream r = new(f, FileMode.Open, FileAccess.Read);
                        using BinaryWriter ou = new(new FileStream(Path.ChangeExtension(f, ".out"), FileMode.Create, FileAccess.Write));
                        Export(r, ou, opt);
                    }
                    catch (Exception e) when (e is IOException || e is JsonException)
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
                        Import(r, ou, opt);
                    }
                    catch (Exception e) when (e is IOException || e is JsonException)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
            Console.Error.WriteLine("done, press enter to exit");
            Console.Read();
        }

        private static void SearchParticles(string path)
        {
            Directory.CreateDirectory("./emitters");
            DirectoryInfo i = new(path);
            Dictionary<(int, int), (string, RpPrtStdEmitter)> dict = [];
            Search(i, dict);
            foreach (var kv in dict)
            {
                //Console.WriteLine($"{kv.Key.Item1} {kv.Key.Item2} {kv.Value}");
            }
            Console.Error.WriteLine("done, press enter to exit");
            Console.Read();

            static void Search(DirectoryInfo i, Dictionary<(int, int), (string, RpPrtStdEmitter)> dict)
            {
                foreach (FileInfo f in i.GetFiles())
                {
                    try
                    {
                        using BinaryReader r = new(new FileStream(f.FullName, FileMode.Open, FileAccess.Read));
                        RWFile d = RWFile.Read(r);
                        int emid = 0;
                        foreach (Atomic a in d.Clp!.Atomics)
                        {
                            if (a.Extension.ParticleStandard != null)
                            {
                                foreach (RpPrtStdEmitter em in a.Extension.ParticleStandard.Emitters)
                                {
                                    try
                                    {
                                        using FileStream ou = new($"./emitters/{f.Name}_emitter_{emid}.json", FileMode.Create, FileAccess.Write);
                                        JsonSerializer.Serialize(ou, em, SourceGenerationContext.Default.RpPrtStdEmitter);
                                    }
                                    catch (IOException e)
                                    {
                                        Console.Error.WriteLine(e.ToString());
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e) when (e is IOException || e is JsonException)
                    {
                        //Console.Error.WriteLine(e.ToString());
                    }
                }
                foreach (DirectoryInfo di in i.GetDirectories())
                    Search(di, dict);
            }
        }

        private static void CheckRoundTrip(string path, JsonSerializerOptions opt)
        {
            DirectoryInfo i = new(path);
            Search(i, opt);
            Console.Error.WriteLine("done, press enter to exit");
            Console.Read();

            static void Search(DirectoryInfo i, JsonSerializerOptions opt)
            {
                foreach (FileInfo f in i.GetFiles())
                {
                    try
                    {
                        using BinaryReader r = new(new FileStream(f.FullName, FileMode.Open, FileAccess.Read));
                        using MemoryStream json = new();
                        Import(r, json, opt);
                        if (r.PeekChar() >= 0)
                            throw new IOException("not full input read");
                        json.Position = 0;
                        using DebugWriteCheckStream o = new() { BytesToWrite = File.ReadAllBytes(f.FullName) };
                        {
                            using BinaryWriter w = new(o);
                            Export(json, w, opt);
                        }
                        o.CheckEnd();
                    }
                    catch (Exception e) when (e is IOException || e is JsonException)
                    {
                        Console.Error.WriteLine($"on {f.FullName}:");
                        Console.Error.WriteLine(e.ToString());
                    }
                }
                foreach (DirectoryInfo di in i.GetDirectories())
                    Search(di, opt);
            }
        }

        private static void Import(BinaryReader r, Stream ou, JsonSerializerOptions opt)
        {
            RWFile d = RWFile.Read(r);
            JsonSerializer.Serialize(ou, d, opt);
        }

        private static void Export(Stream r, BinaryWriter ou, JsonSerializerOptions opt)
        {
            RWFile d = JsonSerializer.Deserialize<RWFile>(r, opt) ?? throw new IOException("failed to parse file");
            d.Write(ou);
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RWFile))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    internal class EncodedFloatConverter : JsonConverter<float>
    {
        public unsafe override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.TryGetInt32(out int v))
                throw new JsonException();
            return *(float*)&v;
        }

        public unsafe override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(*(int*)&value);
        }
    }
}