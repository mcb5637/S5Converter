using S5Converter.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal abstract class Extension<T>
    {
        internal abstract int Size(T obj);
        internal abstract bool TryRead(BinaryReader s, ref ChunkHeader h, T obj);
        internal abstract void WriteExt(BinaryWriter s, T obj, UInt32 versionNum, UInt32 buildNum);

        internal int SizeH(T obj)
        {
            return ChunkHeader.Size + Size(obj);
        }
        internal void Read(BinaryReader s, T obj)
        {
            ChunkHeader exheader = ChunkHeader.FindChunk(s, RwCorePluginID.EXTENSION);
            while (exheader.Length > 0)
            {
                ChunkHeader h = ChunkHeader.Read(s);
                if (!TryRead(s, ref h, obj))
                {
                    Console.Error.WriteLine($"unknown extension {h.Type} on {typeof(T).Name}, skipping");
                    s.ReadBytes(h.Length);
                }
                exheader.Length -= h.Length + 12;
            }
        }
        internal void Write(BinaryWriter s, T obj, UInt32 versionNum, UInt32 buildNum)
        {
            new ChunkHeader()
            {
                Length = Size(obj),
                Type = RwCorePluginID.EXTENSION,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            WriteExt(s, obj, versionNum, buildNum);
        }

        // extensions:
        // camera & light: userdata
    }

    [JsonConverter(typeof(EnumJsonConverter<RwBlendFunction>))]
    internal enum RwBlendFunction : int
    {
        rwBLENDNABLEND = 0,
        rwBLENDZERO,            /**<(0,    0,    0,    0   ) */
        rwBLENDONE,             /**<(1,    1,    1,    1   ) */
        rwBLENDSRCCOLOR,        /**<(Rs,   Gs,   Bs,   As  ) */
        rwBLENDINVSRCCOLOR,     /**<(1-Rs, 1-Gs, 1-Bs, 1-As) */
        rwBLENDSRCALPHA,        /**<(As,   As,   As,   As  ) */
        rwBLENDINVSRCALPHA,     /**<(1-As, 1-As, 1-As, 1-As) */
        rwBLENDDESTALPHA,       /**<(Ad,   Ad,   Ad,   Ad  ) */
        rwBLENDINVDESTALPHA,    /**<(1-Ad, 1-Ad, 1-Ad, 1-Ad) */
        rwBLENDDESTCOLOR,       /**<(Rd,   Gd,   Bd,   Ad  ) */
        rwBLENDINVDESTCOLOR,    /**<(1-Rd, 1-Gd, 1-Bd, 1-Ad) */
        rwBLENDSRCALPHASAT,     /**<(f,    f,    f,    1   )  f = min (As, 1-Ad) */
    };

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

        [JsonInclude]
        public MeshHeaderFlags Flags;
        [JsonInclude]
        public RpMesh[] Meshes = [];

        internal struct RpMesh
        {
            [JsonInclude]
            public int MaterialIndex;
            [JsonInclude]
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

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
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

    internal class RpSkin
    {
        internal struct RwMatrixWeights
        {
            [JsonInclude]
            public float w0;
            [JsonInclude]
            public float w1;
            [JsonInclude]
            public float w2;
            [JsonInclude]
            public float w3;

            internal const int Size = sizeof(float) * 4;

            internal static RwMatrixWeights Read(BinaryReader s)
            {
                return new()
                {
                    w0 = s.ReadSingle(),
                    w1 = s.ReadSingle(),
                    w2 = s.ReadSingle(),
                    w3 = s.ReadSingle(),
                };
            }
            internal readonly void Write(BinaryWriter s)
            {
                s.Write(w0);
                s.Write(w1);
                s.Write(w2);
                s.Write(w3);
            }
        };
        internal struct Split
        {
            [JsonInclude]
            public int BoneLimit;
            [JsonInclude]
            public int[]? MeshBoneRemapIndices;
            [JsonInclude]
            public int[]? MeshBoneRLECount;
            [JsonInclude]
            public int[]? MeshBoneRLE;
        }

        [JsonInclude]
        public int MaxWeight;
        [JsonInclude]
        public int[] UsedBones = [];
        [JsonInclude]
        public int[] VertexBoneIndices = [];
        [JsonInclude]
        public RwMatrixWeights[] VertexBoneWeights = [];
        [JsonInclude]
        public RwMatrixRaw[] SkinToBoneMatrices = [];
        [JsonInclude]
        public Split SplitData;

        internal int GetSize(RpGeometry g)
        {
            if (g.Flags.Native)
                throw new IOException("geometry skin native not supported");
            int r = sizeof(byte) * 4;
            r += UsedBones.Length * sizeof(byte);
            r += VertexBoneIndices.Length * sizeof(int);
            r += VertexBoneWeights.Length * RwMatrixWeights.Size;
            r += SkinToBoneMatrices.Length * RwMatrixRaw.Size;
            r += sizeof(int) * 3;
            r += SplitData.MeshBoneRemapIndices?.Length ?? 0 * sizeof(byte);
            r += SplitData.MeshBoneRLECount?.Length ?? 0 * sizeof(byte);
            r += SplitData.MeshBoneRLE?.Length ?? 0 * sizeof(byte);
            return r;
        }
        internal int GetSizeH(RpGeometry g) => GetSize(g) + ChunkHeader.Size;

        internal static RpSkin Read(BinaryReader s, RpGeometry g, bool header)
        {
            if (g.Flags.Native) // seems to not be used
                throw new IOException("geometry skin native not supported");
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.SKINPLUGIN);
            int nVert = g.NVerts;
            int nBones = s.ReadByte();
            int nUsedBones = s.ReadByte();
            RpSkin r = new()
            {
                MaxWeight = s.ReadByte(),
                UsedBones = new int[nUsedBones],
                VertexBoneIndices = new int[nVert],
                VertexBoneWeights = new RwMatrixWeights[nVert],
                SkinToBoneMatrices = new RwMatrixRaw[nBones],
            };
            s.ReadByte();
            r.UsedBones.ReadArray(() => s.ReadByte());
            r.VertexBoneIndices.ReadArray(s.ReadInt32);
            r.VertexBoneWeights.ReadArray(s, RwMatrixWeights.Read);
            r.SkinToBoneMatrices.ReadArray(s, RwMatrixRaw.Read);

            {
                r.SplitData.BoneLimit = s.ReadInt32();
                int numMeshes = s.ReadInt32();
                int numRLE = s.ReadInt32();
                if (numMeshes > 0)
                {
                    r.SplitData.MeshBoneRemapIndices = new int[nBones];
                    r.SplitData.MeshBoneRemapIndices.ReadArray(() => s.ReadByte());
                    r.SplitData.MeshBoneRLECount = new int[2 * numMeshes];
                    r.SplitData.MeshBoneRLECount.ReadArray(() => s.ReadByte());
                    r.SplitData.MeshBoneRLE = new int[2 * numRLE];
                    r.SplitData.MeshBoneRLE.ReadArray(() => s.ReadByte());
                }
            }


            return r;
        }

        internal void Write(BinaryWriter s, RpGeometry g, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            if (g.Flags.Native)
                throw new IOException("geometry skin native not supported");
            if (header)
            {
                new ChunkHeader()
                {
                    Length = GetSize(g),
                    Type = RwCorePluginID.SKINPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }

            int nVert = g.NVerts;
            if (VertexBoneIndices.Length != nVert)
                throw new IOException("VertexBoneIndices length missmatch");
            if (VertexBoneWeights.Length != nVert)
                throw new IOException("VertexBoneWeights length missmatch");

            s.WriteAsByte(SkinToBoneMatrices.Length);
            s.WriteAsByte(UsedBones.Length);
            s.WriteAsByte(MaxWeight);
            s.Write((byte)0);
            foreach (int b in UsedBones)
                s.WriteAsByte(b);
            foreach (int b in VertexBoneIndices)
                s.Write(b);
            foreach (RwMatrixWeights b in VertexBoneWeights)
                b.Write(s);
            foreach (RwMatrixRaw b in SkinToBoneMatrices)
                b.Write(s);

            {
                s.Write(SplitData.BoneLimit);
                s.Write((SplitData.MeshBoneRLECount?.Length ?? 0) / 2);
                s.Write((SplitData.MeshBoneRLE?.Length ?? 0) / 2);
                if (SplitData.MeshBoneRLECount != null)
                {
                    if (SplitData.MeshBoneRemapIndices == null || SplitData.MeshBoneRLE == null)
                        throw new IOException("SplitData must either be fully populated or not at all");
                    if (SplitData.MeshBoneRemapIndices.Length != SkinToBoneMatrices.Length)
                        throw new IOException("MeshBoneRemapIndices length missmatch");
                    foreach (int i in SplitData.MeshBoneRemapIndices)
                        s.WriteAsByte(i);
                    foreach (int i in SplitData.MeshBoneRLECount)
                        s.WriteAsByte(i);
                    foreach (int i in SplitData.MeshBoneRLE)
                        s.WriteAsByte(i);
                }
                else
                {
                    if (SplitData.MeshBoneRemapIndices != null || SplitData.MeshBoneRLE != null)
                        throw new IOException("SplitData must either be fully populated or not at all");
                }
            }
        }
    }

    internal class RpMorphGeometry
    {
        internal struct RpMorphInterpolator
        {
            [JsonInclude]
            public int Flags;
            [JsonInclude]
            public int StartMorphTarget;
            [JsonInclude]
            public int EndMorphTarget;
            [JsonInclude]
            public float Time;
            [JsonInclude]
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

        [JsonInclude]
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

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
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
