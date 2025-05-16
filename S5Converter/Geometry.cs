using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Geometry : IJsonOnDeserialized
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
                readonly get => ((int)(Flag & RpGeometryFlag.NumTexCoordSetsStoredInFlags)) >> 16;
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
                    else if (value >= 0 && value < ((int)RpGeometryFlag.NumTexCoordSetsStoredInFlags) >> 16)
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
        [JsonInclude]
        public MorphTarget[] MorphTargets = [];
        [JsonPropertyName("textureCoordinates")]
        [JsonInclude]
        public TexCoord[][] TextureCoordinates = [];
        [JsonPropertyName("format")]
        [JsonInclude]
        public GeometryFlagS Flags = new();
        [JsonPropertyName("triangles")]
        [JsonInclude]
        public Triangle[] Triangles = [];
        [JsonPropertyName("materials")]
        [JsonInclude]
        public Material[] Materials = [];
        [JsonPropertyName("preLitLum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        public RGBA[]? PreLitLum;

        [JsonPropertyName("extension")]
        [JsonInclude]
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
                    r += (PreLitLum?.Length ?? 0) * RGBA.Size + TextureCoordinates.Sum(x => x.Length) * TexCoord.Size;
                    r += Triangles.Length * Triangle.Size;
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

        internal static Geometry Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.GEOMETRY);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            Geometry r = new()
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
                    r.TextureCoordinates = new TexCoord[nTexCoordSets][];
                    for (int i = 0; i < nTexCoordSets; ++i)
                    {
                        r.TextureCoordinates[i] = new TexCoord[nVert];
                        for (int j = 0; j < nVert; ++j)
                            r.TextureCoordinates[i][j] = TexCoord.Read(s);
                    }
                }
                if (nTri > 0)
                {
                    r.Triangles = new Triangle[nTri];
                    for (int i = 0; i < nTri; ++i)
                        r.Triangles[i] = Triangle.Read(s);
                }
            }

            if (nMorphT > 0)
            {
                r.MorphTargets = new MorphTarget[nMorphT];
                for (int i = 0; i < nMorphT; ++i)
                    r.MorphTargets[i] = MorphTarget.Read(s, nVert);
            }

            ChunkHeader.FindChunk(s, RwCorePluginID.MATLIST);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            int nMateri = s.ReadInt32();
            int[] materialClone = new int[nMateri];
            r.Materials = new Material[nMateri];
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
                    r.Materials[i] = Material.Read(s, true);
                }
            }

            r.Extension.Read(s, r);

            return r;
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
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
                foreach (TexCoord[] t in TextureCoordinates)
                {
                    if (t.Length != nVerts)
                        throw new IOException("vertex number missmatch");
                    foreach (TexCoord tc in t)
                    {
                        tc.Write(s);
                    }
                }

                foreach (Triangle t in Triangles)
                    t.Write(s);
            }

            foreach (MorphTarget t in MorphTargets)
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
            foreach (Material m in Materials)
                m.Write(s, true, versionNum, buildNum);

            Extension.Write(s, this, versionNum, buildNum);
        }

        public void OnDeserialized()
        {
            MorphTargets ??= [];
            TextureCoordinates ??= [];
            Triangles ??= [];
            Materials ??= [];
            Extension ??= new();
        }
    }

    internal struct RGBA
    {
        [JsonPropertyName("red")]
        [JsonInclude]
        public byte Red;
        [JsonPropertyName("green")]
        [JsonInclude]
        public byte Green;
        [JsonPropertyName("blue")]
        [JsonInclude]
        public byte Blue;
        [JsonPropertyName("alpha")]
        [JsonInclude]
        public byte Alpha;

        internal const int Size = 4 * sizeof(byte);

        internal static RGBA Read(BinaryReader s)
        {
            return new()
            {
                Red = s.ReadByte(),
                Green = s.ReadByte(),
                Blue = s.ReadByte(),
                Alpha = s.ReadByte(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(Red);
            s.Write(Green);
            s.Write(Blue);
            s.Write(Alpha);
        }
    }
    internal struct RGBAF
    {
        [JsonPropertyName("red")]
        [JsonInclude]
        public float Red;
        [JsonPropertyName("green")]
        [JsonInclude]
        public float Green;
        [JsonPropertyName("blue")]
        [JsonInclude]
        public float Blue;
        [JsonPropertyName("alpha")]
        [JsonInclude]
        public float Alpha;

        internal const int Size = 4 * sizeof(float);

        internal static RGBAF Read(BinaryReader s)
        {
            return new()
            {
                Red = s.ReadSingle(),
                Green = s.ReadSingle(),
                Blue = s.ReadSingle(),
                Alpha = s.ReadSingle(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(Red);
            s.Write(Green);
            s.Write(Blue);
            s.Write(Alpha);
        }
    }


    internal struct TexCoord
    {
        [JsonPropertyName("u")]
        [JsonInclude]
        public float U;
        [JsonPropertyName("v")]
        [JsonInclude]
        public float V;

        internal const int Size = 2 * sizeof(float);

        internal static TexCoord Read(BinaryReader s)
        {
            return new()
            {
                U = s.ReadSingle(),
                V = s.ReadSingle(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(U);
            s.Write(V);
        }
    }
    internal struct Triangle
    {
        [JsonPropertyName("v1")]
        [JsonInclude]
        public Int16 V1;
        [JsonPropertyName("v2")]
        [JsonInclude]
        public Int16 V2;
        [JsonPropertyName("v3")]
        [JsonInclude]
        public Int16 V3;
        [JsonPropertyName("materialId")]
        [JsonInclude]
        public Int16 MaterialId;

        internal const int Size = sizeof(Int16) * 4;

        internal static Triangle Read(BinaryReader s)
        {
            return new() // original code reads as is and then swaps around
            {
                V2 = s.ReadInt16(),
                V1 = s.ReadInt16(),
                MaterialId = s.ReadInt16(),
                V3 = s.ReadInt16(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(V2);
            s.Write(V1);
            s.Write(MaterialId);
            s.Write(V3);
        }
    }
    internal struct MorphTarget
    {
        [JsonPropertyName("vertices")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Vec3[]? Verts;
        [JsonPropertyName("normals")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Vec3[]? Normals;
        [JsonPropertyName("sphere")]
        [JsonInclude]
        public Sphere BoundingSphere;

        internal readonly int NumVerts => Verts?.Length ?? Normals?.Length ?? 0;

        internal readonly int Size => (Verts?.Length ?? 0) * Vec3.Size + (Normals?.Length ?? 0) * Vec3.Size + Sphere.Size + sizeof(int) * 2;

        internal static MorphTarget Read(BinaryReader s, int numVert)
        {
            MorphTarget r = new()
            {
                BoundingSphere = Sphere.Read(s),
            };
            bool hasverts = s.ReadInt32() != 0;
            bool hasnorm = s.ReadInt32() != 0;
            if (hasverts)
            {
                r.Verts = new Vec3[numVert];
                for (int i = 0; i < numVert; i++)
                    r.Verts[i] = Vec3.Read(s);
            }
            if (hasnorm)
            {
                r.Normals = new Vec3[numVert];
                for (int i = 0; i < numVert; i++)
                    r.Normals[i] = Vec3.Read(s);
            }
            return r;
        }

        internal readonly void Write(BinaryWriter s, int nvert)
        {
            BoundingSphere.Write(s);
            s.Write(Verts == null ? 0 : 1);
            s.Write(Normals == null ? 0 : 1);
            if (Verts != null)
            {
                if (nvert != Verts.Length)
                    throw new IOException("morphtarget vertex number missmatch");
                foreach (var v in Verts)
                    v.Write(s);
            }
            if (Normals != null)
            {
                if (nvert != Normals.Length)
                    throw new IOException("morphtarget normals number missmatch");
                foreach (var n in Normals)
                    n.Write(s);
            }
        }
    }

    internal class Texture : IJsonOnDeserialized
    {
        [JsonPropertyName("texture")]
        [JsonInclude]
        public string Tex = "";
        [JsonInclude]
        public int[]? TexPadding;
        [JsonPropertyName("textureAlpha")]
        [JsonInclude]
        public string TextureAlpha = "";
        [JsonInclude]
        public int[]? TextureAlphaPadding;
        [JsonInclude]
        public Int16 FilterAddressing;
        [JsonInclude]
        public Int16 UnusedInt1;

        [JsonPropertyName("extension")]
        [JsonInclude]
        public TextureExtension Extension = new();

        public Texture() { }

        internal int Size => ChunkHeader.Size + ChunkHeader.GetStringSize(Tex) + ChunkHeader.GetStringSize(TextureAlpha)
            + 2 * sizeof(Int16) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static Texture Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.TEXTURE);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 2 * sizeof(Int16))
                throw new IOException("Texture struct invalid length");
            Texture r = new()
            {
                FilterAddressing = s.ReadInt16(),
                UnusedInt1 = s.ReadInt16(),
            };
            (r.Tex, r.TexPadding) = ChunkHeader.FindAndReadString(s);
            (r.TextureAlpha, r.TextureAlphaPadding) = ChunkHeader.FindAndReadString(s);
            r.Extension.Read(s, r);

            return r;
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            if (ChunkHeader.GetStringSize(Tex) > 0x80)
                throw new IOException("Texture name too long");
            if (ChunkHeader.GetStringSize(TextureAlpha) > 0x80)
                throw new IOException("Texture name too long");
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.TEXTURE,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 2 * sizeof(Int16),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(FilterAddressing);
            s.Write(UnusedInt1);
            ChunkHeader.WriteString(s, Tex, TexPadding, versionNum, buildNum);
            ChunkHeader.WriteString(s, TextureAlpha, TextureAlphaPadding, versionNum, buildNum);
            Extension.Write(s, this, versionNum, buildNum);
        }

        internal static int OptTextureSize(Texture? t)
        {
            if (t == null)
            {
                return sizeof(int);
            }
            else
            {
                return sizeof(int) + t.SizeH;
            }
        }
        internal static Texture? ReadOptText(BinaryReader s)
        {
            if (s.ReadInt32() == 0)
            {
                return null;
            }
            else
            {
                return Texture.Read(s, true);
            }
        }
        internal static void WriteOptTexture(BinaryWriter s, ref Texture? t, UInt32 versionNum, UInt32 buildNum)
        {
            if (t == null)
            {
                s.Write(0);
            }
            else
            {
                s.Write(1);
                t.Write(s, true, versionNum, buildNum);
            }
        }

        public void OnDeserialized()
        {
            Tex ??= "";
            TextureAlpha ??= "";
            Extension ??= new();
        }
    }
    internal struct SurfaceProperties
    {
        [JsonPropertyName("ambient")]
        [JsonInclude]
        public float Ambient;
        [JsonPropertyName("specular")]
        [JsonInclude]
        public float Specular;
        [JsonPropertyName("diffuse")]
        [JsonInclude]
        public float Diffuse;

        internal const int Size = sizeof(float) * 3;
    }
    internal class Material : IJsonOnDeserialized
    {
        [JsonInclude]
        public int UnknownInt1;
        [JsonPropertyName("color")]
        [JsonInclude]
        public RGBA Color;
        [JsonInclude]
        public int UnknownInt2;
        [JsonInclude]
        public SurfaceProperties SurfaceProps;
        [JsonPropertyName("textures")]
        [JsonInclude]
        public Texture[] Textures = [];

        [JsonPropertyName("extension")]
        [JsonInclude]
        public MaterialExtension Extension = new();

        internal int Size => ChunkHeader.Size + RGBA.Size + sizeof(int) * 3 + SurfaceProperties.Size + Textures.Sum(t => t.SizeH) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static Material Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATERIAL);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 7 * sizeof(int))
                throw new IOException("Material struct invalid length");
            Material m = new()
            {
                UnknownInt1 = s.ReadInt32(),
                Color = RGBA.Read(s),
                UnknownInt2 = s.ReadInt32(),
            };
            bool hastex = s.ReadInt32() != 0;
            m.SurfaceProps.Ambient = s.ReadSingle();
            m.SurfaceProps.Specular = s.ReadSingle();
            m.SurfaceProps.Diffuse = s.ReadSingle();
            if (hastex)
            {
                m.Textures = [Texture.Read(s, true)];
            }

            m.Extension.Read(s, m);

            return m;
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATERIAL,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 7 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(UnknownInt1);
            Color.Write(s);
            s.Write(UnknownInt2);
            s.Write(Textures.Length > 0 ? 1 : 0);
            s.Write(SurfaceProps.Ambient);
            s.Write(SurfaceProps.Specular);
            s.Write(SurfaceProps.Diffuse);
            foreach (Texture t in Textures)
                t.Write(s, true, versionNum, buildNum);

            Extension.Write(s, this, versionNum, buildNum);
        }

        public void OnDeserialized()
        {
            Textures ??= [];
            Extension ??= new();
        }
    }

    internal class GeometryExtension : Extension<Geometry>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpMeshHeader? BinMeshPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpSkin? SkinPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpMorphGeometry? MorphPLG;



        internal override int Size(Geometry obj)
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

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Geometry obj)
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

        internal override void WriteExt(BinaryWriter s, Geometry obj, UInt32 versionNum, UInt32 buildNum)
        {
            BinMeshPLG?.Write(s, true, versionNum, buildNum);
            SkinPLG?.Write(s, obj, true, versionNum, buildNum);
            MorphPLG?.Write(s, true, versionNum, buildNum);
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
        }
    }

    internal class TextureExtension : Extension<Texture>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        internal override int Size(Texture obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Texture obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, Texture obj, UInt32 versionNum, UInt32 buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
        }
    }

    internal class MaterialExtension : Extension<Material>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaterialFXMaterial? MaterialFXMat;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RightToRender? RightToRender;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaterialUVAnim? MaterialUVAnim;

        internal override int Size(Material obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (MaterialFXMat != null)
                r += MaterialFXMat.SizeH;
            if (RightToRender != null)
                r += RightToRender.SizeH;
            if (MaterialUVAnim != null)
                r += MaterialUVAnim.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Material obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                case RwCorePluginID.MATERIALEFFECTSPLUGIN:
                    MaterialFXMat = MaterialFXMaterial.Read(s, false);
                    break;
                case RwCorePluginID.RIGHTTORENDER:
                    RightToRender = RightToRender.Read(s, false);
                    break;
                case RwCorePluginID.UVANIMPLUGIN:
                    MaterialUVAnim = MaterialUVAnim.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, Material obj, UInt32 versionNum, UInt32 buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
            MaterialFXMat?.Write(s, true, versionNum, buildNum);
            RightToRender?.Write(s, true, versionNum, buildNum);
            MaterialUVAnim?.Write(s, true, versionNum, buildNum);
        }
    }
}
