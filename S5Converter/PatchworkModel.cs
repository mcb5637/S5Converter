using S5Converter.Atomic;
using S5Converter.CommonExtensions;
using S5Converter.Frame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class PatchworkModel
    {
        internal struct ModelInfo
        {
            public required string Model;
            public RwFrame? Frame;
            public int? ParentToBone;
            public Dictionary<string, RpUserDataArray>? UserDataAdd;
            public RpAtomic.AtomicFlagsS? FlagsOverride;
        }

        public required ModelInfo Main;
        public required ModelInfo[] Extras;
        public required string Output;


        internal void Build(JsonSerializerOptions opt)
        {
            Dictionary<(string, int), int> geom_cache = [];
            var clump = Load(ref Main, opt);
            PostLoad(clump, ref Main, true);

            for (int i = 0; i < clump.Geometries.Length; ++i)
                geom_cache[(Main.Model, i)] = i;

            for (int i = 0; i < Extras.Length; ++i)
            {
                ref var extra_info = ref Extras[i];
                var extra_clump = Load(ref extra_info, opt);
                PostLoad(extra_clump, ref extra_info);
                
                Merge(clump, extra_clump, geom_cache, extra_info.Model);

                if (extra_info.ParentToBone != null)
                {
                    int bone = extra_info.ParentToBone.Value;
                    extra_clump.Frames[0].Frame.ParentFrameIndex = clump.Frames.Index().First(x => x.Item.Extension.HanimPLG?.NodeID == bone).Index;
                }
            }
            if (Output.EndsWith(".json"))
            {
                using FileStream ou = new(Output, FileMode.Create, FileAccess.Write);
                JsonSerializer.Serialize(ou, new RWFile() { Clp = clump }, new SourceGenerationContext(opt).RWFile);
            }
            else
            {
                using BinaryWriter ou = new(new FileStream(Output, FileMode.Create, FileAccess.Write));
                new RWFile() { Clp = clump }.Write(ou);
            }
        }

        private static Clump Load(ref ModelInfo i, JsonSerializerOptions opt)
        {
            RWFile f;
            if (i.Model.EndsWith(".json"))
            {
                using FileStream r = new(i.Model, FileMode.Open, FileAccess.Read);
                f = JsonSerializer.Deserialize<RWFile>(r, new SourceGenerationContext(opt).RWFile) ?? throw new IOException("failed to parse file");
            }
            else
            {
                using BinaryReader r = new(new FileStream(i.Model, FileMode.Open, FileAccess.Read));
                f = RWFile.Read(r, true);
            }
            return f.Clp ?? throw new IOException("no clump");
        }

        private static void Merge(Clump main, Clump extra, Dictionary<(string, int), int> geom_cache, string extra_name)
        {
            var fr = main.Frames.ToList();
            var geom = main.Geometries.ToList();
            var ato = main.Atomics.ToList();
            Dictionary<int, int> frame_reindex = [];
            Dictionary<int, int> geom_reindex = [];
            foreach ((int i, FrameWithExt f) in extra.Frames.Index())
            {
                f.Extension.HanimPLG = null;
                frame_reindex[i] = fr.Count;
                fr.Add(f);
            }
            foreach (FrameWithExt f in extra.Frames)
            {
                if (f.Frame.ParentFrameIndex == -1)
                    f.Frame.ParentFrameIndex = 0;
                else
                    f.Frame.ParentFrameIndex = frame_reindex[f.Frame.ParentFrameIndex];
            }
            foreach ((int i, var g) in extra.Geometries.Index())
            {
                if (geom_cache.TryGetValue((extra_name, i), out int gi))
                {
                    geom_reindex[i] = gi;
                }
                else
                {
                    geom_reindex[i] = geom.Count;
                    geom_cache[(extra_name, i)] = geom.Count;
                    geom.Add(g);
                }
            }
            foreach (var a in extra.Atomics)
            {
                a.FrameIndex = frame_reindex[a.FrameIndex];
                a.GeometryIndex = geom_reindex[a.GeometryIndex];
                ato.Add(a);
            }
            main.Frames = fr.ToArray();
            main.Geometries = geom.ToArray();
            main.Atomics = ato.ToArray();
        }

        private static void PostLoad(Clump c, ref ModelInfo i, bool main = false)
        {
            if (!main)
            {
                if (i.Frame != null)
                {
                    c.Frames[0].Frame = i.Frame;
                    i.Frame.ParentFrameIndex = -1;
                }
            }
            var ud_add = i.UserDataAdd;
            if (ud_add != null)
            {
                foreach (var a in c.Atomics)
                {
                    var f = c.Frames[a.FrameIndex];
                    f.Extension.UserDataPLG ??= [];
                    foreach (var (k, v) in ud_add)
                        f.Extension.UserDataPLG.Add(k, v);
                }
            }
            if (i.FlagsOverride != null)
            {
                foreach (var a in c.Atomics)
                {
                    a.Flags = i.FlagsOverride.Value;
                }
            }
        }
    }
}
