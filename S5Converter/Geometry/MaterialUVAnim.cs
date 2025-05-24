using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class MaterialUVAnim
    {
        public string[] Name = [];

        internal const int FixedSizeString = 32;

        private int DataSize => Name.Length * FixedSizeString + sizeof(int);
        internal int Size => ChunkHeader.Size + DataSize;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static MaterialUVAnim Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.UVANIMPLUGIN);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nanims = s.ReadInt32();
            MaterialUVAnim r = new()
            {
                Name = new string[nanims],
            };
            r.Name.ReadArray(() => s.ReadFixedSizeString(FixedSizeString));
            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Type = RwCorePluginID.UVANIMPLUGIN,
                    Length = Size,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Type = RwCorePluginID.STRUCT,
                Length = DataSize,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(Name.Length);
            foreach (string n in Name)
                s.WriteFixedSizeString(n, FixedSizeString);
        }
    }
}
