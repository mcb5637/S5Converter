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
        internal int NumAtomics, NumCameras, NumLights;
        [JsonPropertyName("frames")]
        [JsonInclude]
        public FrameWithExt[] Frames = [];
        [JsonPropertyName("geometries")]
        [JsonInclude]
        public Geometry[] Geometries = [];

        internal static Clump Read(BinaryReader s)
        {
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            Clump c = new()
            {
                NumAtomics = s.ReadInt32(),
                NumLights = s.ReadInt32(),
                NumCameras = s.ReadInt32()
            };

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
                c.Frames[i].Extension = Extension.Read(s);
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


            return c;
        }
        internal void Write(BinaryWriter s)
        {

        }
    }
}
