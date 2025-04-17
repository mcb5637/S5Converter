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

        internal readonly int Size => ChunkHeader.Size + sizeof(int) * 4 + Extension.SizeH(RwCorePluginID.ATOMIC);
        internal readonly int SizeH => Size + ChunkHeader.Size;


        internal static Atomic Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.ATOMIC);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 4 * sizeof(int))
                throw new IOException("atomic read invalid struct length");
            Atomic a = new()
            {
                FrameIndex = s.ReadInt32(),
                GeometryIndex = s.ReadInt32(),
                Flags = s.ReadInt32(),
                UnknownInt1 = s.ReadInt32(),
                Extension = Extension.Read(s, RwCorePluginID.ATOMIC),
            };
            return a;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.ATOMIC,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 4 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(FrameIndex);
            s.Write(GeometryIndex);
            s.Write(Flags);
            s.Write(UnknownInt1);
            Extension.Write(s, RwCorePluginID.ATOMIC);
        }
    }
}
