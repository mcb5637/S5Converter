using System.Text.Json.Serialization;

namespace S5Converter.Anim
{
    internal class RtCompressedAnim : RtAnimAnimation
    {
        internal struct RtCompressedKeyFrame
        {
            public required float Time;
            public required RtQuat Q;
            public required Vec3 T;
            public required int PrevKeyFrame;

            internal const int Size = sizeof(float) * 2 + sizeof(short) * 7;

            private static unsafe float Uncompress(ushort i)
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
            private static unsafe ushort Compress(float f)
            {
                uint r = 0;
                int* data = (int*)&f;
                if ((*data & 0xFFF) >= 0x800)
                {
                    int v1 = *data >> 23 & 0xFF;
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
                r = v3 >> 16 & 0x8000;
                if ((v3 & 0x7FFFFFFF) != 0)
                    r |= v3 >> 12 & 0x7FF | (v3 >> 23) - 0x10 << 11;
                return (ushort)r;
            }

            internal static RtCompressedKeyFrame Read(BinaryReader s)
            {
                RtCompressedKeyFrame r = new()
                {
                    Time = s.ReadSingle(),
                    Q = new()
                    {
                        Imaginary = new()
                        {
                            X = Uncompress(s.ReadUInt16()),
                            Y = Uncompress(s.ReadUInt16()),
                            Z = Uncompress(s.ReadUInt16()),
                        },
                        Real = Uncompress(s.ReadUInt16()),
                    },
                    T = new()
                    {
                        X = Uncompress(s.ReadUInt16()),
                        Y = Uncompress(s.ReadUInt16()),
                        Z = Uncompress(s.ReadUInt16()),
                    },
                    PrevKeyFrame = s.ReadInt32(),
                };
                if (r.Time > 0)
                    r.PrevKeyFrame /= 24;
                return r;
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(Compress(Q.Imaginary.X));
                s.Write(Compress(Q.Imaginary.Y));
                s.Write(Compress(Q.Imaginary.Z));
                s.Write(Compress(Q.Real));
                s.Write(Compress(T.X));
                s.Write(Compress(T.Y));
                s.Write(Compress(T.Z));
                s.Write(PrevKeyFrame * (Time > 0 ? 24 : 1));
            }
        }

        [JsonRequired]
        public Vec3 Offset;
        [JsonRequired]
        public Vec3 Scalar;
        [JsonRequired]
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

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            CheckType();
            WriteA(s, KeyFrames.Length, header ? Size : -1, versionNum, buildNum);

            foreach (RtCompressedKeyFrame kf in KeyFrames)
                kf.Write(s);

            Offset.Write(s);
            Scalar.Write(s);
        }
    }
}
