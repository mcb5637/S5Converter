using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Clump
    {
        [JsonPropertyName("frames")]
        [JsonInclude]
        public FrameWithExt[] Frames = [];
        [JsonPropertyName("atomics")]
        [JsonInclude]
        public Atomic[] Atomics = [];
        [JsonPropertyName("geometries")]
        [JsonInclude]
        public Geometry[] Geometries = [];

        [JsonPropertyName("extension")]
        [JsonInclude]
        public Extension Extension = new();


        private int GeometryListSize => sizeof(int) + Geometries.Sum(x => x.SizeH);
        private int FrameListSize => sizeof(int) + Frames.Sum(x => Frame.Size + x.Extension.SizeH(RwCorePluginID.FRAMELIST));
        internal int Size => ChunkHeader.Size * 5 + sizeof(int) * 3 + FrameListSize + GeometryListSize + Atomics.Sum(x => x.SizeH) + Extension.SizeH(RwCorePluginID.CLUMP);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static Clump Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.CLUMP);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nAtomics = s.ReadInt32();
            int nLights = s.ReadInt32();
            int nCameras = s.ReadInt32();
            Clump c = new();

            // framelist
            ChunkHeader.FindChunk(s, RwCorePluginID.FRAMELIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nframes = s.ReadInt32();
            c.Frames = new FrameWithExt[nframes];
            for (int i = 0; i < nframes; ++i)
            {
                Frame f = Frame.Read(s);
                c.Frames[i] = new()
                {
                    Frame = f,
                };
            }
            for (int i = 0; i < nframes; ++i)
            {
                c.Frames[i].Extension = Extension.Read(s, RwCorePluginID.FRAMELIST);
            }

            // geometrylist
            ChunkHeader.FindChunk(s, RwCorePluginID.GEOMETRYLIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nGeoms = s.ReadInt32();
            c.Geometries = new Geometry[nGeoms];
            for (int i = 0; i < nGeoms; ++i)
            {
                c.Geometries[i] = Geometry.Read(s, true);
            }

            // atomics
            c.Atomics = new Atomic[nAtomics];
            for (int i = 0; i < nAtomics; ++i)
            {
                if (nGeoms == 0) // TODO
                    throw new IOException("trying to read atomic without geometry in clump. inline geometry not supportet at the moment!");
                c.Atomics[i] = Atomic.Read(s, true);
            }

            if (nLights > 0)
                throw new IOException("lights not supported!");

            if (nCameras > 0)
                throw new IOException("cameras not supported");

            c.Extension = Extension.Read(s, RwCorePluginID.CLUMP);

            return c;
        }
        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.CLUMP,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 3 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(Atomics.Length);
            s.Write(0);
            s.Write(0);

            // framelist
            new ChunkHeader()
            {
                Length = FrameListSize + ChunkHeader.Size,
                Type = RwCorePluginID.FRAMELIST,
            }.Write(s);
            new ChunkHeader()
            {
                Length = Frame.Size * Frames.Length + sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(Frames.Length);
            foreach (FrameWithExt f in Frames)
                f.Frame.Write(s);
            foreach (FrameWithExt f in Frames)
                f.Extension.Write(s, RwCorePluginID.FRAMELIST);

            // geometrylist
            new ChunkHeader()
            {
                Length = GeometryListSize + ChunkHeader.Size,
                Type = RwCorePluginID.GEOMETRYLIST,
            }.Write(s);
            new ChunkHeader()
            {
                Length = sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(Geometries.Length);
            foreach (Geometry g in Geometries)
                g.Write(s, true);

            // atomics
            foreach (Atomic a in Atomics)
                a.Write(s, true);


            // extension
            Extension.Write(s, RwCorePluginID.CLUMP);
        }
    }
}
