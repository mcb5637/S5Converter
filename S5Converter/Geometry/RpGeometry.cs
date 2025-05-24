using S5Converter.Atomic;
using S5Converter.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter.Geometry
{
    internal class RpGeometry
    {
        public struct GeometryFlagS
        {
            [Flags]
            internal enum RpGeometryFlag : int
            {
                rpGEOMETRYTRISTRIP = 0x00000001,
                rpGEOMETRYPOSITIONS = 0x00000002,
                rpGEOMETRYTEXTURED = 0x00000004,
                rpGEOMETRYPRELIT = 0x00000008,
                rpGEOMETRYNORMALS = 0x00000010,
                rpGEOMETRYLIGHT = 0x00000020,
                rpGEOMETRYMODULATEMATERIALCOLOR = 0x00000040,
                rpGEOMETRYTEXTURED2 = 0x00000080,
                rpGEOMETRYNATIVE = 0x01000000,
                rpGEOMETRYNATIVEINSTANCE = 0x02000000,

                NumTexCoordSetsStoredInFlags = 0xFF0000,
                rpGEOMETRYFLAGSMASK = 0x000000FF,
                rpGEOMETRYNATIVEFLAGSMASK = 0x0F000000,
            };
            internal RpGeometryFlag Flag;

            public bool TriStrip
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYTRISTRIP);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYTRISTRIP);
            }
            public bool Positions
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYPOSITIONS);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYPOSITIONS);
            }
            [JsonIgnore]
            public bool NumberOfTexture1
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYTEXTURED);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYTEXTURED);
            }
            [JsonIgnore]
            public bool NumberOfTexture2
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYTEXTURED2);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYTEXTURED2);
            }
            [JsonIgnore]
            public int NumberOfTextureEncoded
            {
                readonly get => (int)(Flag & RpGeometryFlag.NumTexCoordSetsStoredInFlags) >> 16;
                set {
                    Flag.SetFlag(false, RpGeometryFlag.NumTexCoordSetsStoredInFlags);
                    Flag |= (RpGeometryFlag)(value << 16);
                }
            }
            public int NumTextureCoordinates
            {
                readonly get
                {
                    if (NumberOfTextureEncoded > 0)
                        return NumberOfTextureEncoded;
                    if (NumberOfTexture2)
                        return 2;
                    if (NumberOfTexture1)
                        return 1;
                    return 0;
                }
                set
                {
                    if (value == 0)
                    {
                        NumberOfTexture1 = false;
                        NumberOfTexture2 = false;
                        NumberOfTextureEncoded = 0;
                    }
                    else if (value == 1)
                    {
                        NumberOfTexture1 = true;
                        NumberOfTexture2 = false;
                        NumberOfTextureEncoded = 1;
                    }
                    else if (value == 2)
                    {
                        NumberOfTexture1 = false;
                        NumberOfTexture2 = true;
                        NumberOfTextureEncoded = 2;
                    }
                    else if (value >= 0 && value < (int)RpGeometryFlag.NumTexCoordSetsStoredInFlags >> 16)
                    {
                        NumberOfTexture1 = false;
                        NumberOfTexture2 = false;
                        NumberOfTextureEncoded = value;
                    }
                    else
                    {
                        throw new IOException("invalid number of textures");
                    }
                }
            }
            public bool PreLit
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYPRELIT);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYPRELIT);
            }
            public bool Normals
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYNORMALS);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYNORMALS);
            }
            public bool Light
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYLIGHT);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYLIGHT);
            }
            public bool ModulateMaterialColor
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYMODULATEMATERIALCOLOR);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYMODULATEMATERIALCOLOR);
            }
            public bool Native
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYNATIVE);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYNATIVE);
            }
            public bool NativeInstance
            {
                readonly get => Flag.HasFlag(RpGeometryFlag.rpGEOMETRYNATIVEINSTANCE);
                set => Flag.SetFlag(value, RpGeometryFlag.rpGEOMETRYNATIVEINSTANCE);
            }
        }

        [JsonPropertyName("morphTargets")]
        public RpMorphTarget[] MorphTargets = [];
        [JsonPropertyName("textureCoordinates")]
        public RwTexCoords[][] TextureCoordinates = [];
        [JsonPropertyName("format")]
        public GeometryFlagS Flags = new();
        [JsonPropertyName("triangles")]
        public RpTriangle[] Triangles = [];
        [JsonPropertyName("materials")]
        public RpMaterial[] Materials = [];
        [JsonPropertyName("preLitLum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RGBA[]? PreLitLum;

        [JsonPropertyName("extension")]
        public GeometryExtension Extension = new();


        private int MaterialListSize => sizeof(int) + sizeof(int) * Materials.Length + Materials.Sum(x => x.SizeH);

        internal int NVerts
        {
            get
            {
                if (TextureCoordinates.Length > 0)
                    return TextureCoordinates[0].Length;
                else if (MorphTargets.Length > 0)
                    return MorphTargets[0].NumVerts;
                else
                    return 0;
            }
        }

        private int SizeActual
        {
            get
            {
                int r = sizeof(int) * 4;
                if (!Flags.Native)
                {
                    r += (PreLitLum?.Length ?? 0) * RGBA.Size + TextureCoordinates.Sum(x => x.Length) * RwTexCoords.Size;
                    r += Triangles.Length * RpTriangle.Size;
                }
                r += MorphTargets.Sum(x => x.Size);
                return r;
            }
        }
        internal int Size
        {
            get
            {
                int r = SizeActual + ChunkHeader.Size;
                r += ChunkHeader.Size * 2;
                r += MaterialListSize;
                r += Extension.SizeH(this);
                return r;
            }
        }
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpGeometry Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.GEOMETRY);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            RpGeometry r = new()
            {
                Flags = new()
                {
                    Flag = (GeometryFlagS.RpGeometryFlag)s.ReadInt32(),
                },
            };
            int nTri = s.ReadInt32();
            int nVert = s.ReadInt32();
            int nMorphT = s.ReadInt32();
            int nTexCoordSets = r.Flags.NumTextureCoordinates;
            if (!r.Flags.Native)
            {
                if (r.Flags.PreLit)
                {
                    r.PreLitLum = new RGBA[nVert];
                    for (int i = 0; i < nVert; ++i)
                        r.PreLitLum[i] = RGBA.Read(s);
                }
                if (nTexCoordSets > 0)
                {
                    r.TextureCoordinates = new RwTexCoords[nTexCoordSets][];
                    for (int i = 0; i < nTexCoordSets; ++i)
                    {
                        r.TextureCoordinates[i] = new RwTexCoords[nVert];
                        for (int j = 0; j < nVert; ++j)
                            r.TextureCoordinates[i][j] = RwTexCoords.Read(s);
                    }
                }
                if (nTri > 0)
                {
                    r.Triangles = new RpTriangle[nTri];
                    for (int i = 0; i < nTri; ++i)
                        r.Triangles[i] = RpTriangle.Read(s);
                }
            }

            if (nMorphT > 0)
            {
                r.MorphTargets = new RpMorphTarget[nMorphT];
                for (int i = 0; i < nMorphT; ++i)
                    r.MorphTargets[i] = RpMorphTarget.Read(s, nVert);
            }

            ChunkHeader.FindChunk(s, RwCorePluginID.MATLIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nMateri = s.ReadInt32();
            int[] materialClone = new int[nMateri];
            r.Materials = new RpMaterial[nMateri];
            for (int i = 0; i < nMateri; ++i)
                materialClone[i] = s.ReadInt32();
            for (int i = 0; i < nMateri; ++i)
            {
                if (materialClone[i] >= 0)
                {
                    r.Materials[i] = r.Materials[materialClone[i]];
                }
                else
                {
                    r.Materials[i] = RpMaterial.Read(s, true);
                }
            }

            r.Extension.Read(s, r);

            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.GEOMETRY,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = SizeActual,
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);

            Flags.NumTextureCoordinates = TextureCoordinates.Length;
            Flags.PreLit = PreLitLum != null;

            int nTexCoordSets = Flags.NumTextureCoordinates;

            int nVerts = NVerts;

            s.Write((int)Flags.Flag);
            s.Write(Triangles.Length);
            s.Write(nVerts);
            s.Write(MorphTargets.Length);


            if (!Flags.Native)
            {
                if (Flags.PreLit)
                {
                    if (PreLitLum == null)
                        throw new IOException("prelit null, but flag set");
                    foreach (RGBA l in PreLitLum)
                        l.Write(s);
                }

                if (TextureCoordinates.Length != nTexCoordSets)
                    throw new IOException("tex coord set number missmatch");
                foreach (RwTexCoords[] t in TextureCoordinates)
                {
                    if (t.Length != nVerts)
                        throw new IOException("vertex number missmatch");
                    foreach (RwTexCoords tc in t)
                    {
                        tc.Write(s);
                    }
                }

                foreach (RpTriangle t in Triangles)
                    t.Write(s);
            }

            foreach (RpMorphTarget t in MorphTargets)
                t.Write(s, nVerts);

            new ChunkHeader()
            {
                Length = MaterialListSize + ChunkHeader.Size,
                Type = RwCorePluginID.MATLIST,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            new ChunkHeader()
            {
                Length = sizeof(int) + sizeof(int) * Materials.Length,
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(Materials.Length);
            for (int i = 0; i < Materials.Length; ++i)
                s.Write(-1);
            foreach (RpMaterial m in Materials)
                m.Write(s, true, versionNum, buildNum);

            Extension.Write(s, this, versionNum, buildNum);
        }
    }

    internal class GeometryExtension : Extension<RpGeometry>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpMeshHeader? BinMeshPLG;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpSkin? SkinPLG;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpMorphGeometry? MorphPLG;



        internal override int Size(RpGeometry obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (BinMeshPLG != null)
                r += BinMeshPLG.SizeH;
            if (SkinPLG != null)
                r += SkinPLG.GetSizeH(obj);
            if (MorphPLG != null)
                r += MorphPLG.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RpGeometry obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                case RwCorePluginID.BINMESHPLUGIN:
                    BinMeshPLG = RpMeshHeader.Read(s, false);
                    break;
                case RwCorePluginID.SKINPLUGIN:
                    SkinPLG = RpSkin.Read(s, obj, false);
                    break;
                case RwCorePluginID.MORPHPLUGIN:
                    MorphPLG = RpMorphGeometry.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, RpGeometry obj, uint versionNum, uint buildNum)
        {
            BinMeshPLG?.Write(s, true, versionNum, buildNum);
            SkinPLG?.Write(s, obj, true, versionNum, buildNum);
            MorphPLG?.Write(s, true, versionNum, buildNum);
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
        }
    }

}
