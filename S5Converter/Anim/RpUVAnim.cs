using System.Text.Json.Serialization;

namespace S5Converter.Anim
{
    internal class RpUVAnim : RtAnimAnimation, IDictEntry<RpUVAnim>
    {
        internal struct RpUVAnimParamKeyFrameData
        {
            public required float Time;
            public required float Thetha;
            public required float S0;
            public required float S1;
            public required float Skew;
            public required float X;
            public required float Y;
            public required int PrevKeyFrame;

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
            public required float Time;
            public required Vec2 Right;
            public required Vec2 Up;
            public required Vec2 Pos;
            public required int PrevKeyFrame;

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

        [JsonRequired]
        public string Name = "";
        [JsonRequired]
        public uint[] NodeToUVChannelMap = [];
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpUVAnimParamKeyFrameData[]? ParamKeyFrames;
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

        public void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
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
}
