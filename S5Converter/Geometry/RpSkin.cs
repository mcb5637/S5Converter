using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class RpSkin
    {
        internal struct RwMatrixWeights
        {
            public float w0;
            public float w1;
            public float w2;
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
            public int BoneLimit;
            public int[]? MeshBoneRemapIndices;
            public int[]? MeshBoneRLECount;
            public int[]? MeshBoneRLE;
        }

        public int MaxWeight;
        public int[] UsedBones = [];
        public int[] VertexBoneIndices = [];
        public RwMatrixWeights[] VertexBoneWeights = [];
        public RwMatrixRaw[] SkinToBoneMatrices = [];
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

        internal void Write(BinaryWriter s, RpGeometry g, bool header, uint versionNum, uint buildNum)
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
}
