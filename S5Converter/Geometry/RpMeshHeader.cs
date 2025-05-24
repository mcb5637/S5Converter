using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class RpMeshHeader
    {
        internal struct MeshHeaderFlags
        {
            internal enum RpMeshHeaderFlags : int
            {
                rpMESHHEADERTRISTRIP = 0x0001,
                rpMESHHEADERTRIFAN = 0x0002,
                rpMESHHEADERLINELIST = 0x0004,
                rpMESHHEADERPOLYLINE = 0x0008,
                rpMESHHEADERPOINTLIST = 0x0010,

                rpMESHHEADERPRIMMASK = 0x00FF,
                rpMESHHEADERUNINDEXED = 0x0100,
            };
            [JsonConverter(typeof(EnumJsonConverter<MeshType>))]
            internal enum MeshType : int
            {
                TriList,
                TriStrip,
                TriFan,
                LineList,
                PolyLine,
                PointList,
            }

            internal RpMeshHeaderFlags Flag;

            public bool UnIndexed
            {
                readonly get => Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERUNINDEXED);
                set => Flag.SetFlag(value, RpMeshHeaderFlags.rpMESHHEADERUNINDEXED);
            }
            public MeshType Type
            {
                readonly get
                {
                    if (Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERTRISTRIP))
                        return MeshType.TriStrip;
                    if (Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERTRIFAN))
                        return MeshType.TriFan;
                    if (Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERLINELIST))
                        return MeshType.LineList;
                    if (Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERPOLYLINE))
                        return MeshType.PolyLine;
                    if (Flag.HasFlag(RpMeshHeaderFlags.rpMESHHEADERPOINTLIST))
                        return MeshType.PointList;
                    return MeshType.TriList;
                }
                set
                {
                    Flag.SetFlag(false, RpMeshHeaderFlags.rpMESHHEADERPRIMMASK);
                    switch (value)
                    {
                        case MeshType.TriList:
                            break;
                        case MeshType.TriStrip:
                            Flag.SetFlag(true, RpMeshHeaderFlags.rpMESHHEADERTRISTRIP);
                            break;
                        case MeshType.TriFan:
                            Flag.SetFlag(true, RpMeshHeaderFlags.rpMESHHEADERTRIFAN);
                            break;
                        case MeshType.LineList:
                            Flag.SetFlag(true, RpMeshHeaderFlags.rpMESHHEADERLINELIST);
                            break;
                        case MeshType.PolyLine:
                            Flag.SetFlag(true, RpMeshHeaderFlags.rpMESHHEADERPOLYLINE);
                            break;
                        case MeshType.PointList:
                            Flag.SetFlag(true, RpMeshHeaderFlags.rpMESHHEADERPOINTLIST);
                            break;
                    }
                }
            }
        }

        public MeshHeaderFlags Flags;
        public RpMesh[] Meshes = [];

        internal struct RpMesh
        {
            public int MaterialIndex;
            public int[] VertexIndices;

            internal readonly int Size => sizeof(int) * 2 + sizeof(int) * VertexIndices.Length;
        }

        internal int Size => sizeof(int) * 3 + Meshes.Sum(x => x.Size);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpMeshHeader Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.BINMESHPLUGIN);
            RpMeshHeader r = new()
            {
                Flags = new()
                {
                    Flag = (MeshHeaderFlags.RpMeshHeaderFlags)s.ReadInt32(),
                },
            };
            int numMeshes = s.ReadInt32();
            _ = s.ReadInt32(); // total indices

            r.Meshes = new RpMesh[numMeshes];
            for (int i = 0; i < numMeshes; ++i)
            {
                int nIndices = s.ReadInt32();
                r.Meshes[i] = new RpMesh()
                {
                    MaterialIndex = s.ReadInt32(),
                    VertexIndices = new int[nIndices],
                };
                for (int j = 0; j < nIndices; ++j)
                    r.Meshes[i].VertexIndices[j] = s.ReadInt32();
            }

            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.BINMESHPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write((int)Flags.Flag);
            s.Write(Meshes.Length);
            s.Write(Meshes.Sum(x => x.VertexIndices.Length));
            foreach (RpMesh m in Meshes)
            {
                s.Write(m.VertexIndices.Length);
                s.Write(m.MaterialIndex);
                foreach (int v in m.VertexIndices)
                    s.Write(v);
            }
        }
    }
}
