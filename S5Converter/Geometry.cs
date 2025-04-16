using System;
using System.Collections.Generic;
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

        internal static Geometry Read(BinaryReader s)
        {
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
                    ChunkHeader.FindChunk(s, RwCorePluginID.MATERIAL);
                    r.Materials[i] = Material.Read(s);
                }
            }

            r.Extension = Extension.Read(s, RwCorePluginID.GEOMETRY);

            return r;
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
    }
    internal struct TexCoord
    {
        [JsonPropertyName("u")]
        [JsonInclude]
        public float U;
        [JsonPropertyName("v")]
        [JsonInclude]
        public float V;

        internal static TexCoord Read(BinaryReader s)
        {
            return new()
            {
                U = s.ReadSingle(),
                V = s.ReadSingle(),
            };
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

        internal static Texture Read(BinaryReader s)
        {
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 1 * 4)
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

        // on writing: ensure string length < 0x80 (read buffer)
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

        internal static Material Read(BinaryReader s)
        {
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 7 * 4)
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
                ChunkHeader.FindChunk(s, RwCorePluginID.TEXTURE);
                m.Textures = [Texture.Read(s)];
            }

            m.Extension = Extension.Read(s, RwCorePluginID.MATERIAL);

            return m;
        }
    }
}
