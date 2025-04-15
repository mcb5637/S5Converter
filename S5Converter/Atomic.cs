using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal struct Atomic
    {
        [JsonPropertyName("frameIndex")]
        [JsonInclude]
        public int FrameIndex;
        [JsonPropertyName("geometryIndex")]
        [JsonInclude]
        public int GeometryIndex;
        [JsonInclude]
        public int Flags;
        [JsonInclude]
        public int UnknownInt1;

        [JsonPropertyName("extension")]
        [JsonInclude]
        public Extension Extension;


        internal static Atomic Read(BinaryReader s)
        {
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 4 * 4)
                throw new IOException("atomic read invalid struct length");
            Atomic a = new()
            {
                FrameIndex = s.ReadInt32(),
                GeometryIndex = s.ReadInt32(),
                Flags = s.ReadInt32(),
                UnknownInt1 = s.ReadInt32(),
                Extension = Extension.Read(s),
            };
            return a;
        }
    }
}
