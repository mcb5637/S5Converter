using S5Converter.Atomic;
using S5Converter.CommonExtensions;
using S5Converter.Frame;
using System.Text.Json;

namespace S5Converter;

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
        Dictionary<(string, int), int> geomCache = [];
        var clump = Load(ref Main, opt);
        PostLoad(clump, ref Main, true);

        for (int i = 0; i < clump.Geometries.Length; ++i)
            geomCache[(Main.Model, i)] = i;

        for (int i = 0; i < Extras.Length; ++i)
        {
            ref var extraInfo = ref Extras[i];
            var extraClump = Load(ref extraInfo, opt);
            PostLoad(extraClump, ref extraInfo);

            Merge(clump, extraClump, geomCache, extraInfo.Model);

            if (extraInfo.ParentToBone != null)
            {
                int bone = extraInfo.ParentToBone.Value;
                extraClump.Frames[0].Frame.ParentFrameIndex =
                    clump.Frames.Index().First(x => x.Item.Extension.HanimPLG?.NodeID == bone).Index;
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
            f = JsonSerializer.Deserialize<RWFile>(r, new SourceGenerationContext(opt).RWFile) ??
                throw new IOException("failed to parse file");
        }
        else
        {
            using BinaryReader r = new(new FileStream(i.Model, FileMode.Open, FileAccess.Read));
            f = RWFile.Read(r, true);
        }

        return f.Clp ?? throw new IOException("no clump");
    }

    private static void Merge(Clump main, Clump extra, Dictionary<(string, int), int> geomCache, string extraName)
    {
        var fr = main.Frames.ToList();
        var geom = main.Geometries.ToList();
        var ato = main.Atomics.ToList();
        Dictionary<int, int> frameReindex = [];
        Dictionary<int, int> geomReindex = [];
        foreach ((int i, FrameWithExt f) in extra.Frames.Index())
        {
            f.Extension.HanimPLG = null;
            frameReindex[i] = fr.Count;
            fr.Add(f);
        }

        foreach (FrameWithExt f in extra.Frames)
        {
            f.Frame.ParentFrameIndex = f.Frame.ParentFrameIndex == -1 ? 0 : frameReindex[f.Frame.ParentFrameIndex];
        }

        foreach ((int i, var g) in extra.Geometries.Index())
        {
            if (geomCache.TryGetValue((extraName, i), out int gi))
            {
                geomReindex[i] = gi;
            }
            else
            {
                geomReindex[i] = geom.Count;
                geomCache[(extraName, i)] = geom.Count;
                geom.Add(g);
            }
        }

        foreach (var a in extra.Atomics)
        {
            a.FrameIndex = frameReindex[a.FrameIndex];
            a.GeometryIndex = geomReindex[a.GeometryIndex];
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

        var udAdd = i.UserDataAdd;
        if (udAdd != null)
        {
            foreach (var a in c.Atomics)
            {
                var f = c.Frames[a.FrameIndex];
                f.Extension.UserDataPLG ??= [];
                foreach (var (k, v) in udAdd)
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