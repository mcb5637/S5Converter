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

        internal static Clump Read(BinaryReader s)
        {
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
                Frame f = new()
                {
                    Right = Vec3.Read(s),
                    Up = Vec3.Read(s),
                    At = Vec3.Read(s),
                    Position = Vec3.Read(s),
                    ParentFrameIndex = s.ReadInt32(),
                    UnknownIntProbablyUnused = s.ReadInt32()
                };
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
                ChunkHeader.FindChunk(s, RwCorePluginID.GEOMETRY);
                c.Geometries[i] = Geometry.Read(s);
            }

            // atomics
            c.Atomics = new Atomic[nAtomics];
            for (int i = 0; i < nAtomics; ++i)
            {
                ChunkHeader.FindChunk(s, RwCorePluginID.ATOMIC);
                if (nGeoms == 0) // TODO
                    throw new IOException("trying to read atomic without geometry in clump. inline geometry not supportet at the moment!");
                c.Atomics[i] = Atomic.Read(s);
            }

            if (nLights > 0)
                throw new IOException("lights not supported!");

            if (nCameras > 0)
                throw new IOException("cameras not supported");

            c.Extension = Extension.Read(s, RwCorePluginID.CLUMP);

            return c;
        }
        internal void Write(BinaryWriter s)
        {

        }
    }
}
