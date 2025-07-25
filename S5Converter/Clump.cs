﻿using S5Converter.Atomic;
using S5Converter.Frame;
using S5Converter.Geometry;
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
        public required FrameWithExt[] Frames;
        [JsonPropertyName("atomics")]
        public required RpAtomic[] Atomics;
        [JsonPropertyName("geometries")]
        public required RpGeometry[] Geometries;
        public RpLight_WithFrameIndex[] Lights = [];

        [JsonPropertyName("extension")]
        public ClumpExtension Extension = new();


        private int GeometryListSize => sizeof(int) + Geometries.Sum(x => x.SizeH);
        private int FrameListSize => sizeof(int) + Frames.Sum(x => RwFrame.Size + x.Extension.SizeH(x.Frame));
        internal int Size => ChunkHeader.Size * 5 + sizeof(int) * 3 + FrameListSize + GeometryListSize + Atomics.Sum(x => x.SizeH) + Lights.Sum(x => x.Size) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static Clump Read(BinaryReader s, bool header, bool convertRad)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.CLUMP);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nAtomics = s.ReadInt32();
            int nLights = s.ReadInt32();
            int nCameras = s.ReadInt32();
            Clump c = new()
            {
                Frames = [],
                Atomics = [],
                Geometries = [],
            };

            // framelist
            ChunkHeader.FindChunk(s, RwCorePluginID.FRAMELIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nframes = s.ReadInt32();
            c.Frames = new FrameWithExt[nframes];
            for (int i = 0; i < nframes; ++i)
            {
                RwFrame f = RwFrame.Read(s);
                c.Frames[i] = new()
                {
                    Frame = f,
                };
            }
            for (int i = 0; i < nframes; ++i)
            {
                c.Frames[i].Extension.Read(s, c.Frames[i].Frame);
            }

            // geometrylist
            ChunkHeader.FindChunk(s, RwCorePluginID.GEOMETRYLIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nGeoms = s.ReadInt32();
            c.Geometries = new RpGeometry[nGeoms];
            for (int i = 0; i < nGeoms; ++i)
            {
                c.Geometries[i] = RpGeometry.Read(s, true);
            }

            // atomics
            c.Atomics = new RpAtomic[nAtomics];
            for (int i = 0; i < nAtomics; ++i)
            {
                if (nGeoms == 0) // TODO
                    throw new IOException("trying to read atomic without geometry in clump. inline geometry not supportet at the moment!");
                c.Atomics[i] = RpAtomic.Read(s, true, convertRad);
            }

            c.Lights = new RpLight_WithFrameIndex[nLights];
            for (int i = 0; i < nLights; ++i)
            {
                c.Lights[i] = RpLight_WithFrameIndex.Read(s);
            }

            if (nCameras > 0)
                throw new IOException("cameras not supported");

            c.Extension.Read(s, c);

            return c;
        }
        internal void Write(BinaryWriter s, bool header, bool convertRad, UInt32 versionNum, UInt32 buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.CLUMP,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 3 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(Atomics.Length);
            s.Write(Lights.Length);
            s.Write(0);

            // framelist
            RpHAnimHierarchy.RebuildNodeHierarchy(Frames);
            new ChunkHeader()
            {
                Length = FrameListSize + ChunkHeader.Size,
                Type = RwCorePluginID.FRAMELIST,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            new ChunkHeader()
            {
                Length = RwFrame.Size * Frames.Length + sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(Frames.Length);
            foreach (FrameWithExt f in Frames)
                f.Frame.Write(s);
            foreach (FrameWithExt f in Frames)
                f.Extension.Write(s, f.Frame, versionNum, buildNum);

            // geometrylist
            new ChunkHeader()
            {
                Length = GeometryListSize + ChunkHeader.Size,
                Type = RwCorePluginID.GEOMETRYLIST,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            new ChunkHeader()
            {
                Length = sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(Geometries.Length);
            foreach (RpGeometry g in Geometries)
                g.Write(s, true, versionNum, buildNum);

            // atomics
            foreach (RpAtomic a in Atomics)
                a.Write(s, true, convertRad, versionNum, buildNum);

            // lights
            foreach (RpLight_WithFrameIndex l in Lights)
                l.Write(s, versionNum, buildNum);

            // extension
            Extension.Write(s, this, versionNum, buildNum);
        }
    }

    internal class ClumpExtension : Extension<Clump>
    {
        internal override int Size(Clump obj)
        {
            return 0;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Clump obj)
        {
            return false;
        }

        internal override void WriteExt(BinaryWriter s, Clump obj, UInt32 versionNum, UInt32 buildNum)
        {
            
        }
    }
}
