using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Geometry
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

        [JsonPropertyName("morphTargets")]
        [JsonInclude]
        public MorphTarget[] MorphTargets = [];
        [JsonPropertyName("textureCoordinates")]
        [JsonInclude]
        public TexCoord[][] TextureCoordinates = [];
        [JsonPropertyName("format")]
        [JsonInclude]
        public RpGeometryFlag Flags;
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
        public Extension Extension = new();


        private int MaterialListSize => sizeof(int) + sizeof(int) * Materials.Length + Materials.Sum(x => x.SizeH);

        private int SizeActual
        {
            get
            {
                int r = sizeof(int) * 4;
                if (!Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYNATIVE))
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
                r += Extension.SizeH(RwCorePluginID.GEOMETRY);
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
                Flags = (RpGeometryFlag)s.ReadInt32(),
            };
            int nTri = s.ReadInt32();
            int nVert = s.ReadInt32();
            int nMorphT = s.ReadInt32();
            int nTexCoordSets;
            if (r.Flags.IsFlagSet(RpGeometryFlag.NumTexCoordSetsStoredInFlags))
                nTexCoordSets = ((int)(r.Flags & RpGeometryFlag.NumTexCoordSetsStoredInFlags)) >> 16;
            else if (r.Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYTEXTURED2))
                nTexCoordSets = 2;
            else if (r.Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYTEXTURED))
                nTexCoordSets = 1;
            else
                nTexCoordSets = 0;
            if (!r.Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYNATIVE))
            {
                if (r.Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYPRELIT))
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

            r.Extension = Extension.Read(s, RwCorePluginID.GEOMETRY);

            return r;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.GEOMETRY,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = SizeActual,
                Type = RwCorePluginID.STRUCT,
            }.Write(s);

            int nTexCoordSets;
            if (Flags.IsFlagSet(RpGeometryFlag.NumTexCoordSetsStoredInFlags))
                nTexCoordSets = ((int)(Flags & RpGeometryFlag.NumTexCoordSetsStoredInFlags)) >> 16;
            else if (Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYTEXTURED2))
                nTexCoordSets = 2;
            else if (Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYTEXTURED))
                nTexCoordSets = 1;
            else
                nTexCoordSets = 0;

            int nVerts;
            if (nTexCoordSets > 0)
                nVerts = TextureCoordinates[0].Length;
            else if (MorphTargets.Length > 0)
                nVerts = MorphTargets[0].NumVerts;
            else
                nVerts = 0;

            s.Write((int)Flags);
            s.Write(Triangles.Length);
            s.Write(nVerts);
            s.Write(MorphTargets.Length);


            if (!Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYNATIVE))
            {
                if (Flags.IsFlagSet(RpGeometryFlag.rpGEOMETRYPRELIT))
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
            }.Write(s);
            new ChunkHeader()
            {
                Length = sizeof(int) + sizeof(int) * Materials.Length,
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(Materials.Length);
            for (int i = 0; i < Materials.Length; ++i)
                s.Write(-1);
            foreach (Material m in Materials)
                m.Write(s, true);

            Extension.Write(s, RwCorePluginID.GEOMETRY);
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

        internal int NumVerts => Verts?.Length ?? Normals?.Length ?? 0;

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

    internal struct Texture
    {
        [JsonPropertyName("texture")]
        [JsonInclude]
        public string Tex = "";
        [JsonPropertyName("textureAlpha")]
        [JsonInclude]
        public string TextureAlpha = "";
        [JsonInclude]
        public Int16 FilterAddressing;
        [JsonInclude]
        public Int16 UnusedInt1;

        [JsonPropertyName("extension")]
        [JsonInclude]
        public Extension Extension = new();

        public Texture() { }

        internal readonly int Size => ChunkHeader.Size + ChunkHeader.GetStringSize(Tex) + ChunkHeader.GetStringSize(TextureAlpha)
            + 2 * sizeof(Int16) + Extension.SizeH(RwCorePluginID.TEXTURE);
        internal readonly int SizeH => Size + ChunkHeader.Size;

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
                Tex = ChunkHeader.FindAndReadString(s),
                TextureAlpha = ChunkHeader.FindAndReadString(s),
                Extension = Extension.Read(s, RwCorePluginID.TEXTURE),
            };

            return r;
        }

        internal void Write(BinaryWriter s, bool header)
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
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 2 * sizeof(Int16),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(FilterAddressing);
            s.Write(UnusedInt1);
            ChunkHeader.WriteString(s, Tex);
            ChunkHeader.WriteString(s, TextureAlpha);
            Extension.Write(s, RwCorePluginID.TEXTURE);
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
    internal class Material
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
        public Extension Extension = new();

        internal int Size => ChunkHeader.Size + RGBA.Size + sizeof(int) * 3 + SurfaceProperties.Size + Textures.Sum(t => t.SizeH) + Extension.SizeH(RwCorePluginID.MATERIAL);
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

            m.Extension = Extension.Read(s, RwCorePluginID.MATERIAL);

            return m;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATERIAL,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 7 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(UnknownInt1);
            Color.Write(s);
            s.Write(UnknownInt2);
            s.Write(Textures.Length > 0 ? 1 : 0);
            s.Write(SurfaceProps.Ambient);
            s.Write(SurfaceProps.Specular);
            s.Write(SurfaceProps.Diffuse);
            foreach (Texture t in Textures)
                t.Write(s, true);

            Extension.Write(s, RwCorePluginID.MATERIAL);
        }
    }
}
