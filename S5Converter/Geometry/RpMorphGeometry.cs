using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class RpMorphGeometry
    {
        internal struct RpMorphInterpolator
        {
            public int Flags;
            public int StartMorphTarget;
            public int EndMorphTarget;
            public float Time;
            public int NextMorphTarget;

            internal const int Size = sizeof(int) * 5;

            internal static RpMorphInterpolator Read(BinaryReader s)
            {
                return new()
                {
                    Flags = s.ReadInt32(),
                    StartMorphTarget = s.ReadInt32(),
                    EndMorphTarget = s.ReadInt32(),
                    Time = s.ReadSingle(),
                    NextMorphTarget = s.ReadInt32(),
                };
            }

            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Flags);
                s.Write(StartMorphTarget);
                s.Write(EndMorphTarget);
                s.Write(Time);
                s.Write(NextMorphTarget);
            }
        }

        public RpMorphInterpolator[] Interpolators = [];

        internal int Size => Interpolators.Length * RpMorphInterpolator.Size + sizeof(int);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpMorphGeometry Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MORPHPLUGIN);
            int nInter = s.ReadInt32();
            RpMorphGeometry r = new()
            {
                Interpolators = new RpMorphInterpolator[nInter],
            };
            r.Interpolators.ReadArray(s, RpMorphInterpolator.Read);
            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Type = RwCorePluginID.MORPHPLUGIN,
                    Length = Size,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(Interpolators.Length);
            foreach (RpMorphInterpolator i in Interpolators)
                i.Write(s);
        }
    }
}
