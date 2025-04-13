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
                Frame f = new();
                f.Right.X = s.ReadSingle();
                f.Right.Y = s.ReadSingle();
                f.Right.Z = s.ReadSingle();
                f.Up.X = s.ReadSingle();
                f.Up.Y = s.ReadSingle();
                f.Up.Z = s.ReadSingle();
                f.At.X = s.ReadSingle();
                f.At.Y = s.ReadSingle();
                f.At.Z = s.ReadSingle();
                f.Position.X = s.ReadSingle();
                f.Position.Y = s.ReadSingle();
                f.Position.Z = s.ReadSingle();
                f.ParentFrameIndex = s.ReadInt32();
                f.UnknownIntProbablyUnused = s.ReadInt32();
                c.Frames[i] = new()
                {
                    Frame = f,
                };
            }
            for (int i = 0; i < nframes; ++i)
            {
                c.Frames[i].Extension = Extension.Read(s);
            }



            return c;
        }
        internal void Write(BinaryWriter s)
        {

        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Clump))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
