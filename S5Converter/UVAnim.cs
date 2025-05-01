using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal enum AnimType
    {
        UVAnimLinear = 0x1C0,
        UVAnimParam = 0x1C1,
    }
    internal abstract class RtAnimAnimation
    {
        [JsonInclude]
        public AnimType InterpolatorTypeId;
        [JsonInclude]
        public int Flags;
        [JsonInclude]
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
        internal void WriteA(BinaryWriter s, int nKeyFrames, int headerSize)
        {
            if (headerSize > 0)
            {
                new ChunkHeader()
                {
                    Type = RwCorePluginID.ANIMANIMATION,
                    Length = headerSize,
                }.Write(s);
            }
            s.Write(256);
            s.Write((int)InterpolatorTypeId);
            s.Write(nKeyFrames);
            s.Write(Flags);
            s.Write(Duration);
        }
    }

    internal class RpUVAnim : RtAnimAnimation, IDictEntry<RpUVAnim>
    {
        internal struct RpUVAnimParamKeyFrameData
        {
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public float Thetha;
            [JsonInclude]
            public float S0;
            [JsonInclude]
            public float S1;
            [JsonInclude]
            public float Skew;
            [JsonInclude]
            public float X;
            [JsonInclude]
            public float Y;
            [JsonInclude]
            public int PrevKeyFrame;

            internal const int Size = sizeof(float) * 8;

            internal static RpUVAnimParamKeyFrameData Read(BinaryReader s)
            {
                return new RpUVAnimParamKeyFrameData()
                {
                    Time = s.ReadSingle(),
                    Thetha = s.ReadSingle(),
                    S0 = s.ReadSingle(),
                    S1 = s.ReadSingle(),
                    Skew = s.ReadSingle(),
                    X = s.ReadSingle(),
                    Y = s.ReadSingle(),
                    PrevKeyFrame = s.ReadInt32(),
                };
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(Thetha);
                s.Write(S0);
                s.Write(S1);
                s.Write(Skew);
                s.Write(X);
                s.Write(Y);
                s.Write(PrevKeyFrame);
            }
        }
        internal struct RpUVAnimLinearKeyFrameData
        {
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public Vec2 Right;
            [JsonInclude]
            public Vec2 Up;
            [JsonInclude]
            public Vec2 Pos;
            [JsonInclude]
            public int PrevKeyFrame;

            internal const int Size = sizeof(float) * 2 + Vec2.Size * 3;

            internal static RpUVAnimLinearKeyFrameData Read(BinaryReader s)
            {
                return new RpUVAnimLinearKeyFrameData()
                {
                    Time = s.ReadSingle(),
                    Right = Vec2.Read(s),
                    Up = Vec2.Read(s),
                    Pos = Vec2.Read(s),
                    PrevKeyFrame = s.ReadInt32(),
                };
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                Right.Write(s);
                Up.Write(s);
                Pos.Write(s);
                s.Write(PrevKeyFrame);
            }
        }

        [JsonInclude]
        public string Name = "";
        [JsonInclude]
        public uint[] NodeToUVChannelMap = [];
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpUVAnimParamKeyFrameData[]? ParamKeyFrames;
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpUVAnimLinearKeyFrameData[]? LinearKeyFrames;

        public static RwCorePluginID DictId => RwCorePluginID.UVANIMDICT;

        private void CheckType()
        {
            if (InterpolatorTypeId != AnimType.UVAnimLinear && InterpolatorTypeId != AnimType.UVAnimParam)
                throw new IOException("invalid anim interpolator type");
        }
        private const int NameFixedStringSize = 32;
        private const int NodeToUVChannelMapSize = 8;

        internal int NKeyFrames => ParamKeyFrames?.Length ?? LinearKeyFrames?.Length ?? 0;
        internal new int Size => RtAnimAnimation.Size + sizeof(byte) * NameFixedStringSize + sizeof(uint) * NodeToUVChannelMapSize + sizeof(int)
            + (ParamKeyFrames?.Length ?? 0) * RpUVAnimParamKeyFrameData.Size + (LinearKeyFrames?.Length ?? 0) * RpUVAnimLinearKeyFrameData.Size;
        [JsonIgnore]
        public int SizeH => Size + ChunkHeader.Size;

        public static RpUVAnim Read(BinaryReader s, bool header)
        {
            RpUVAnim r = new();
            int nkeyframes = r.ReadA(s, header);
            r.CheckType();

            s.ReadInt32();
            r.Name = s.ReadFixedSizeString(NameFixedStringSize);
            r.NodeToUVChannelMap = new uint[NodeToUVChannelMapSize];
            r.NodeToUVChannelMap.ReadArray(s.ReadUInt32);

            if (r.InterpolatorTypeId == AnimType.UVAnimLinear)
            {
                r.LinearKeyFrames = new RpUVAnimLinearKeyFrameData[nkeyframes];
                r.LinearKeyFrames.ReadArray(s, RpUVAnimLinearKeyFrameData.Read);
            }
            else
            {
                r.ParamKeyFrames = new RpUVAnimParamKeyFrameData[nkeyframes];
                r.ParamKeyFrames.ReadArray(s, RpUVAnimParamKeyFrameData.Read);
            }

            return r;
        }

        public void Write(BinaryWriter s, bool header)
        {
            CheckType();
            WriteA(s, NKeyFrames, Size);
            s.Write(0);
            s.WriteFixedSizeString(Name, NameFixedStringSize);
            if (NodeToUVChannelMap.Length != NodeToUVChannelMapSize)
                throw new IOException("NodeToUVChannelMap invalid length");
            foreach (uint m in NodeToUVChannelMap)
                s.Write(m);
            if (InterpolatorTypeId == AnimType.UVAnimLinear)
            {
                if (LinearKeyFrames == null)
                    throw new IOException("no keyframes");
                if (ParamKeyFrames != null)
                    throw new IOException("double keyframes");
                foreach (RpUVAnimLinearKeyFrameData l in LinearKeyFrames)
                    l.Write(s);
            }
            else
            {
                if (ParamKeyFrames == null)
                    throw new IOException("no keyframes");
                if (LinearKeyFrames != null)
                    throw new IOException("double keyframes");
                foreach (RpUVAnimParamKeyFrameData l in ParamKeyFrames)
                    l.Write(s);
            }
        }
    }
}
