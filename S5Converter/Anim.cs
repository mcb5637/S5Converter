using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using static S5Converter.RpUVAnim;
using static S5Converter.RtCompressedAnim;

namespace S5Converter
{
    internal enum AnimType
    {
        HierarchicalAnim = 1,
        CompressedAnim = 2,
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
        internal void WriteA(BinaryWriter s, int nKeyFrames, int headerSize, UInt32 versionNum, UInt32 buildNum)
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

        public void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            CheckType();
            WriteA(s, NKeyFrames, header ? Size : -1, versionNum, buildNum);
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

    internal class RtCompressedAnim : RtAnimAnimation
    {
        internal struct RtCompressedKeyFrame
        {
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public float QX;
            [JsonInclude]
            public float QY;
            [JsonInclude]
            public float QZ;
            [JsonInclude]
            public float QR;
            [JsonInclude]
            public float TX;
            [JsonInclude]
            public float TY;
            [JsonInclude]
            public float TZ;
            [JsonInclude]
            public int PrevKeyFrame;

            internal const int Size = sizeof(float) * 2 + sizeof(Int16) * 7;

            private static unsafe float Uncompress(UInt16 i)
            {
                uint input = i;
                float r = 0.0f;
                uint* data = (uint*)&r;
                *data = (input & 0x8000) << 16;
                if ((input & 0x7fff) != 0)
                {
                    *data |= ((input & 0x7800) << 12) + 0x38000000;
                    *data |= (input & 0x07ff) << 12;
                }
                return r;
            }
            private static unsafe UInt16 Compress(float f)
            {
                uint r = 0;
                int* data = (int*)&f;
                if ((*data & 0xFFF) >= 0x800)
                {
                    int v1 = (*data >> 23) & 0xFF;
                    if (v1 > 0xC && (v1 < 0xFE || (*data & 0x7FF000) != 0x7FF000))
                    {
                        v1 -= 0xC;
                        uint v2 = *(uint*)data & 0x80000000;
                        v1 <<= 23;
                        v1 |= *(int*)&v2;
                        f += *(float*)&v1;
                    }
                }
                uint v3 = *(uint*)data & 0xFFFFF000;
                if ((v3 & 0x7F800000) > 0x3F800000)
                {
                    Console.Error.WriteLine("warning: compressed keyframe float out of range");
                    v3 = v3 & 0xFFFFF000 | 0x7FFFF000;
                }
                if ((v3 & 0x7F800000) < 939524096)
                    v3 &= 0x80000000;
                r = (v3 >> 16) & 0x8000;
                if ((v3 & 0x7FFFFFFF) != 0)
                    r |= (v3 >> 12) & 0x7FF | (((v3 >> 23) - 0x10) << 11);
                return (UInt16)r;
            }

            internal static RtCompressedKeyFrame Read(BinaryReader s)
            {
                RtCompressedKeyFrame r = new()
                {
                    Time = s.ReadSingle(),
                    QX = Uncompress(s.ReadUInt16()),
                    QY = Uncompress(s.ReadUInt16()),
                    QZ = Uncompress(s.ReadUInt16()),
                    QR = Uncompress(s.ReadUInt16()),
                    TX = Uncompress(s.ReadUInt16()),
                    TY = Uncompress(s.ReadUInt16()),
                    TZ = Uncompress(s.ReadUInt16()),
                    PrevKeyFrame = s.ReadInt32(),
                };
                if (r.Time > 0)
                    r.PrevKeyFrame /= 24;
                return r;
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(Compress(QX));
                s.Write(Compress(QY));
                s.Write(Compress(QZ));
                s.Write(Compress(QR));
                s.Write(Compress(TX));
                s.Write(Compress(TY));
                s.Write(Compress(TZ));
                s.Write(PrevKeyFrame * (Time > 0 ? 24 : 1));
            }
        }

        [JsonInclude]
        public Vec3 Offset;
        [JsonInclude]
        public Vec3 Scalar;
        [JsonInclude]
        public RtCompressedKeyFrame[] KeyFrames = [];

        private void CheckType()
        {
            if (InterpolatorTypeId != AnimType.CompressedAnim)
                throw new IOException("invalid anim interpolator type");
        }


        internal new int Size => RtAnimAnimation.Size + Vec3.Size * 2 + KeyFrames.Length * RtCompressedKeyFrame.Size;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RtCompressedAnim Read(BinaryReader s, bool header)
        {
            RtCompressedAnim r = new();
            int nkeyframes = r.ReadA(s, header);

            r.ReadAfterPeek(s, nkeyframes);
            return r;
        }
        internal void ReadAfterPeek(BinaryReader s, int nkeyframes)
        {
            CheckType();

            KeyFrames = new RtCompressedKeyFrame[nkeyframes];
            KeyFrames.ReadArray(s, RtCompressedKeyFrame.Read);

            Offset = Vec3.Read(s);
            Scalar = Vec3.Read(s);
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            CheckType();
            WriteA(s, KeyFrames.Length, header ? Size : -1, versionNum, buildNum);

            foreach (RtCompressedKeyFrame kf in KeyFrames)
                kf.Write(s);

            Offset.Write(s);
            Scalar.Write(s);
        }
    }

    struct RtQuat
    {
        [JsonInclude]
        public Vec3 Imaginary;
        [JsonInclude]
        public float Real;

        internal const int Size = Vec3.Size + sizeof(float);

        internal static RtQuat Read(BinaryReader s)
        {
            return new()
            {
                Imaginary = Vec3.Read(s),
                Real = s.ReadSingle(),
            };
        }
        internal readonly void Write(BinaryWriter s)
        {
            Imaginary.Write(s);
            s.Write(Real);
        }
    }
    class RpHierarchicalAnim : RtAnimAnimation
    {
        internal struct RpHAnimKeyFrame
        {
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public RtQuat Q;
            [JsonInclude]
            public Vec3 T;
            [JsonInclude]
            public int PrevKeyFrame;

            internal const int Size = sizeof(float) * 2 + Vec3.Size + RtQuat.Size;

            internal static RpHAnimKeyFrame Read(BinaryReader s)
            {
                RpHAnimKeyFrame r = new()
                {
                    Time = s.ReadSingle(),
                    Q = RtQuat.Read(s),
                    T = Vec3.Read(s),
                    PrevKeyFrame = s.ReadInt32(),
                };
                if (r.Time > 0)
                {
                    r.PrevKeyFrame /= Size;
                }
                return r;
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                Q.Write(s);
                T.Write(s);
                s.Write(PrevKeyFrame * (Time > 0 ? Size : 1));
            }
        }

        [JsonInclude]
        public RpHAnimKeyFrame[] KeyFrames = [];


        private void CheckType()
        {
            if (InterpolatorTypeId != AnimType.HierarchicalAnim)
                throw new IOException("invalid anim interpolator type");
        }


        internal new int Size => RtAnimAnimation.Size + KeyFrames.Length * RpHAnimKeyFrame.Size;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpHierarchicalAnim Read(BinaryReader s, bool header)
        {
            RpHierarchicalAnim r = new();
            int nkeyframes = r.ReadA(s, header);

            r.ReadAfterPeek(s, nkeyframes);
            return r;
        }
        internal void ReadAfterPeek(BinaryReader s, int nkeyframes)
        {
            CheckType();

            KeyFrames = new RpHAnimKeyFrame[nkeyframes];
            KeyFrames.ReadArray(s, RpHAnimKeyFrame.Read);
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            CheckType();
            WriteA(s, KeyFrames.Length, header ? Size : -1, versionNum, buildNum);

            foreach (RpHAnimKeyFrame kf in KeyFrames)
                kf.Write(s);
        }
    }
}
