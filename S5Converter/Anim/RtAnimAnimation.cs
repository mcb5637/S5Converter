using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace S5Converter.Anim
{
    internal abstract class RtAnimAnimation
    {

        [JsonConverter(typeof(EnumJsonConverter<AnimType>))]
        internal enum AnimType
        {
            HierarchicalAnim = 1,
            CompressedAnim = 2,
            UVAnimLinear = 0x1C0,
            UVAnimParam = 0x1C1,
        }

        [JsonRequired]
        public AnimType InterpolatorTypeId;
        public int Flags = 0;
        [JsonRequired]
        public float Duration;

        internal const int Size = sizeof(int) * 5;

        internal int ReadA(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.ANIMANIMATION);
            if (s.ReadInt32() != 256)
                throw new IOException("anim missing 256 constant");
            InterpolatorTypeId = (AnimType)s.ReadInt32();
            int nframes = s.ReadInt32();
            Flags = s.ReadInt32();
            Duration = s.ReadSingle();
            return nframes;
        }
        internal void WriteA(BinaryWriter s, int nKeyFrames, int headerSize, uint versionNum, uint buildNum)
        {
            if (headerSize > 0)
            {
                new ChunkHeader()
                {
                    Type = RwCorePluginID.ANIMANIMATION,
                    Length = headerSize,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(256);
            s.Write((int)InterpolatorTypeId);
            s.Write(nKeyFrames);
            s.Write(Flags);
            s.Write(Duration);
        }

        internal static RtAnimAnimation ReadAnyAnim(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.ANIMANIMATION);
            if (s.ReadInt32() != 256)
                throw new IOException("anim missing 256 constant");
            AnimType interpolatorTypeId = (AnimType)s.ReadInt32();
            int nframes = s.ReadInt32();
            int flags = s.ReadInt32();
            float duration = s.ReadSingle();

            switch (interpolatorTypeId)
            {
                case AnimType.CompressedAnim:
                    RtCompressedAnim ca = new()
                    {
                        Duration = duration,
                        Flags = flags,
                        InterpolatorTypeId = interpolatorTypeId,
                    };
                    ca.ReadAfterPeek(s, nframes);
                    return ca;
                case AnimType.HierarchicalAnim:
                    RpHierarchicalAnim ha = new()
                    {
                        Duration = duration,
                        Flags = flags,
                        InterpolatorTypeId = interpolatorTypeId,
                    };
                    ha.ReadAfterPeek(s, nframes);
                    return ha;
                default:
                    throw new IOException($"anim type {interpolatorTypeId} is not supported.");
            }
        }
    }
}
