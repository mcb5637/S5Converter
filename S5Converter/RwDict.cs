namespace S5Converter
{
    internal interface IDictEntry<T>
    {
        static abstract RwCorePluginID DictId { get; }
        abstract int SizeH { get; }
        static abstract T Read(BinaryReader s, bool header);
        void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum);
    }
    internal static class RwDict
    {
        internal static int GetSize<T>(T[] data) where T : IDictEntry<T>
        {
            return data.Sum(x => x.SizeH) + ChunkHeader.Size + sizeof(int);
        }
        internal static T[] Read<T>(BinaryReader s, bool header) where T : IDictEntry<T>
        {
            if (header)
                ChunkHeader.FindChunk(s, T.DictId);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != sizeof(int))
                throw new IOException("dict struct length missmatch");
            int nelems = s.ReadInt32();
            T[] r = new T[nelems];
            r.ReadArray(() => T.Read(s, true));
            return r;
        }
        internal static void Write<T>(T[] data, BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum) where T : IDictEntry<T>
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Type = T.DictId,
                    Length = GetSize(data),
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Type = RwCorePluginID.STRUCT,
                Length = sizeof(int),
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(data.Length);
            foreach (T e in data)
                e.Write(s, true, versionNum, buildNum);
        }
    }
}
