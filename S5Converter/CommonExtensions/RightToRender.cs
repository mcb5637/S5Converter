using System.Text.Json.Serialization;

namespace S5Converter.CommonExtensions
{
    internal class RightToRender
    {
        public RwCorePluginID Identifier;
        public int ExtraData;

        internal const int Size = sizeof(int) * 2;
        internal const int SizeH = Size + ChunkHeader.Size;

        internal static RightToRender Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.RIGHTTORENDER);
            return new()
            {
                Identifier = (RwCorePluginID)s.ReadInt32(),
                ExtraData = s.ReadInt32(),
            };
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.RIGHTTORENDER,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write((int)Identifier);
            s.Write(ExtraData);
        }
    }
}
