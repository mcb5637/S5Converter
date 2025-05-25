using System.Text.Json.Serialization;

namespace S5Converter.Anim
{
    class RpHierarchicalAnim : RtAnimAnimation
    {
        internal struct RpHAnimKeyFrame
        {
            public required float Time;
            public required RtQuat Q;
            public required Vec3 T;
            public required int PrevKeyFrame;

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

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            CheckType();
            WriteA(s, KeyFrames.Length, header ? Size : -1, versionNum, buildNum);

            foreach (RpHAnimKeyFrame kf in KeyFrames)
                kf.Write(s);
        }
    }
}
