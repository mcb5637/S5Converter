﻿using S5Converter.Atomic;
using S5Converter.Frame;
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
                IncludeFields = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                RespectNullableAnnotations = true,
                PropertyNameCaseInsensitive = true,
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
                    Import(r, ou, opt, true);
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
            else if (args.Length == 2 && args[0] == "--visualizeHierarchy")
            {
                VisualizeHierarchy(args[1], opt);
                return;
            }
            else if (args.Length == 2 && args[0] == "--patchwork")
            {
                using FileStream r = new(args[1], FileMode.Open, FileAccess.Read);
                PatchworkModel d = JsonSerializer.Deserialize<PatchworkModel>(r, opt) ?? throw new IOException("failed to parse file");
                d.Build(opt);
                Console.Error.WriteLine("done, press enter to exit");
                Console.Read();
                return;
            }
#if DEBUG
            else if (args.Length == 1 && args[0] == "--buildSchema")
            {
                string d = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(RWFile)).ToString();
                File.WriteAllText("./schema.json", d);
                return;
            }
            else if (args.Length >= 2 && args[0] == "--checkRoundTrip")
            {
                opt.WriteIndented = false;
                for (int i = 1; i < args.Length; ++i)
                    CheckRoundTrip(args[i], opt);
                Console.Error.WriteLine("done, press enter to exit");
                Console.Read();
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
                        Import(r, ou, opt, true);
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

        private static void VisualizeHierarchy(string file, JsonSerializerOptions opt)
        {
            try
            {
                RWFile f;
                if (Path.GetExtension(file) == ".json")
                {
                    using FileStream r = new(file, FileMode.Open, FileAccess.Read);
                    f = JsonSerializer.Deserialize<RWFile>(r, opt) ?? throw new IOException("failed to parse file");
                }
                else
                {
                    using BinaryReader r = new(new FileStream(file, FileMode.Open, FileAccess.Read));
                    f = RWFile.Read(r, true);
                }
                if (f.Clp != null)
                {
                    Console.WriteLine("frame hierarchy:");

                    List<HInfo> framehier = HInfo.BuildFrameHierarchy(f.Clp.Frames);
                    if (framehier.Count > 0)
                    {
                        Print(framehier[0], 0);

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("hanim hierarchy:");

                        List<HInfo> hanimhier = HInfo.BuildHAnimHierarchy(f.Clp.Frames);
                        if (hanimhier.Count > 0)
                        {
                            Print(hanimhier[0], 0);

                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("searching missmatches:");
                            Console.Write(HInfo.Check(hanimhier, framehier));
                        }
                    }
                    else
                    {
                        Console.WriteLine("empty frame hierarchy");
                    }
                }
            }
            catch (Exception e) when (e is IOException || e is JsonException)
            {
                Console.Error.WriteLine(e.ToString());
            }

            Console.Error.WriteLine("done, press enter to exit");
            Console.Read();

            static void Print(HInfo e, int lvl)
            {
                for (int i = 0; i < lvl - 1; ++i)
                    Console.Write(" | ");
                if (lvl > 0)
                    Console.Write(" |- ");
                Console.WriteLine(e.ToString());
                foreach (HInfo c in e.Children)
                    Print(c, lvl + 1);
            }
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
                        RWFile d = RWFile.Read(r, true);
                        int emid = 0;
                        foreach (RpAtomic a in d.Clp!.Atomics)
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

        // --encodeFloatAsInt --checkRoundTrip D:\programme\s5\s5complete\graphics\models D:\programme\s5\s5complete\graphics\animations
        private static void CheckRoundTrip(string path, JsonSerializerOptions opt)
        {
            DirectoryInfo i = new(path);
            string[] exclude = ["xd_gold1.dff", "zxdummy.dff", "heads", "piles"];
            Search(i, opt, exclude);

            static void Search(DirectoryInfo i, JsonSerializerOptions opt, string[] exclude)
            {
                foreach (FileInfo f in i.GetFiles())
                {
                    if (exclude.Contains(f.Name))
                        continue;
                    try
                    {
                        using BinaryReader r = new(new FileStream(f.FullName, FileMode.Open, FileAccess.Read));
                        using MemoryStream json = new();
                        RWFile d = Import(r, json, opt, false);
                        if (r.PeekChar() >= 0)
                            throw new IOException("not full input read");
                        if (d.Clp != null)
                        {
                            List<HInfo> framehier = HInfo.BuildFrameHierarchy(d.Clp.Frames);
                            if (framehier.Count > 0)
                            {
                                List<HInfo> hanimhier = HInfo.BuildHAnimHierarchy(d.Clp.Frames);
                                if (hanimhier.Count > 0)
                                {
                                    // remove frame 0 (pos in world), it is intended to have no hainm
                                    framehier.RemoveAll(x => x.FrameIndex == 0);
                                    foreach (HInfo hi in framehier)
                                    {
                                        if (hi.Parent != null && hi.Parent.FrameIndex == 0)
                                            hi.Parent = null;
                                    }
                                    Console.Write(HInfo.Check(hanimhier, framehier));
                                }
                            }
                        }
                        json.Position = 0;
                        using DebugWriteCheckStream o = new() { BytesToWrite = File.ReadAllBytes(f.FullName) };
                        {
                            using BinaryWriter w = new(o);
                            Export(json, w, opt);
                        }
                        o.CheckEnd();
                        // File.WriteAllBytes("./out.json", json.ToArray())
                        // Export(json, new BinaryWriter(new FileStream("./out.dff", FileMode.Create, FileAccess.Write)), opt)
                    }
                    catch (Exception e) when (e is IOException || e is JsonException)
                    {
                        Console.Error.WriteLine($"on {f.FullName}:");
                        Console.Error.WriteLine(e.ToString());
                    }
                }
                foreach (DirectoryInfo di in i.GetDirectories())
                {
                    if (exclude.Contains(di.Name))
                        continue;
                    Search(di, opt, exclude);
                }
            }
        }

        private static RWFile Import(BinaryReader r, Stream ou, JsonSerializerOptions opt, bool convertRad)
        {
            RWFile d = RWFile.Read(r, convertRad);
            JsonSerializer.Serialize(ou, d, opt);
            return d;
        }

        private static void Export(Stream r, BinaryWriter ou, JsonSerializerOptions opt)
        {
            RWFile d = JsonSerializer.Deserialize<RWFile>(r, opt) ?? throw new IOException("failed to parse file");
            d.Write(ou);
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RWFile))]
    [JsonSerializable(typeof(PatchworkModel))]
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