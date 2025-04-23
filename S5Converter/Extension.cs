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
    internal class Extension
    {
        [JsonPropertyName("userDataPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonPropertyName("hanimPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpHAnimHierarchy? HanimPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaterialFXMaterial? MaterialFXMat;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? MaterialFXAtomic_EffectsEnabled;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpMeshHeader? BinMeshPLG;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParticleStandard? ParticleStandard;

        internal int SizeH(RwCorePluginID src)
        {
            return ChunkHeader.Size + Size(src);
        }
        internal int Size(RwCorePluginID src)
        {
            int r = 0;
            if (UserDataPLG != null && src == RwCorePluginID.FRAMELIST)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (HanimPLG != null && src == RwCorePluginID.FRAMELIST)
                r += HanimPLG.SizeH;
            if (MaterialFXMat != null && src == RwCorePluginID.MATERIAL)
                r += MaterialFXMat.SizeH;
            if (MaterialFXAtomic_EffectsEnabled != null && src == RwCorePluginID.ATOMIC)
                r += sizeof(int) + ChunkHeader.Size;
            if (BinMeshPLG != null && src == RwCorePluginID.GEOMETRY)
                r += BinMeshPLG.SizeH;
            if (ParticleStandard != null && src == RwCorePluginID.ATOMIC)
                r += ParticleStandard.SizeH;
            return r;
        }

        internal static Extension Read(BinaryReader s, RwCorePluginID src)
        {
            ChunkHeader exheader = ChunkHeader.FindChunk(s, RwCorePluginID.EXTENSION);
            Extension e = new();
            while (exheader.Length > 0)
            {
                ChunkHeader h = ChunkHeader.Read(s);
                switch ((h.Type, src))
                {
                    case (RwCorePluginID.USERDATAPLUGIN, RwCorePluginID.FRAMELIST):
                        e.UserDataPLG = RpUserDataArray.Read(s, false);
                        break;
                    case (RwCorePluginID.HANIMPLUGIN, RwCorePluginID.FRAMELIST):
                        e.HanimPLG = RpHAnimHierarchy.Read(s, false);
                        break;
                    case (RwCorePluginID.MATERIALEFFECTSPLUGIN, RwCorePluginID.MATERIAL):
                        e.MaterialFXMat = MaterialFXMaterial.Read(s, false);
                        break;
                    case (RwCorePluginID.MATERIALEFFECTSPLUGIN, RwCorePluginID.ATOMIC):
                        e.MaterialFXAtomic_EffectsEnabled = s.ReadInt32() != 0;
                        break;
                    case (RwCorePluginID.BINMESHPLUGIN, RwCorePluginID.GEOMETRY):
                        e.BinMeshPLG = RpMeshHeader.Read(s, false);
                        break;
                    case (RwCorePluginID.PRTSTDPLUGIN, RwCorePluginID.ATOMIC):
                        e.ParticleStandard = ParticleStandard.Read(s, false);
                        break;
                    default:
                        //Console.Error.WriteLine($"unknown extension {(int)h.Type}, skipping");
                        s.ReadBytes((int)h.Length);
                        break;
                }
                exheader.Length -= h.Length + 12;
            }
            return e;
        }

        internal void Write(BinaryWriter s, RwCorePluginID src)
        {
            new ChunkHeader()
            {
                Length = Size(src),
                Type = RwCorePluginID.EXTENSION,
            }.Write(s);
            if (UserDataPLG != null && src == RwCorePluginID.FRAMELIST)
                RpUserDataArray.Write(UserDataPLG, s, true);
            if (HanimPLG != null && src == RwCorePluginID.FRAMELIST)
                HanimPLG.Write(s, true);
            if (MaterialFXMat != null && src == RwCorePluginID.MATERIAL)
                MaterialFXMat.Write(s, true);
            if (MaterialFXAtomic_EffectsEnabled != null && src == RwCorePluginID.ATOMIC)
            {
                new ChunkHeader()
                {
                    Length = sizeof(int),
                    Type = RwCorePluginID.MATERIALEFFECTSPLUGIN,
                }.Write(s);
                s.Write(MaterialFXAtomic_EffectsEnabled.Value ? 1 : 0);
            }
            if (BinMeshPLG != null && src == RwCorePluginID.GEOMETRY)
                BinMeshPLG.Write(s, true);
            if (ParticleStandard != null && src == RwCorePluginID.ATOMIC)
                ParticleStandard.Write(s, true);
        }
    }

    [JsonConverter(typeof(RpUserDataArrayJsonConverter))]
    internal class RpUserDataArray
    {
        internal enum RpUserDataFormat : int
        {
            rpNAUSERDATAFORMAT = 0,
		    rpINTUSERDATA,          /**< 32 bit int data */
		    rpREALUSERDATA,         /**< 32 bit float data */
		    rpSTRINGUSERDATA,       /**< unsigned byte pointer data */
	    };
        internal class DataObj
        {
            public int I;
            public float F;
            public string? S;

            internal int GetSize(RpUserDataFormat f)
            {
                if (f == RpUserDataFormat.rpSTRINGUSERDATA)
                    return S.GetRWLength();
                return sizeof(int); // float/int same size
            }
        }

        public RpUserDataFormat Format;
        public DataObj[] Data = [];

        private int Size => sizeof(int) * 2 + Data.Sum(x => x.GetSize(Format));
        internal static int GetSize(Dictionary<string, RpUserDataArray> d)
        {
            return sizeof(int) + d.Sum(x => x.Key.GetRWLength() + x.Value.Size);
        }
        internal static int GetSizeH(Dictionary<string, RpUserDataArray> d)
        {
            return GetSize(d) + ChunkHeader.Size;
        }

        internal static Dictionary<string, RpUserDataArray> Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.USERDATAPLUGIN);
            Dictionary<string, RpUserDataArray> r = [];
            int numUD = s.ReadInt32();
            for (int i = 0; i < numUD; ++i)
            {
                string udname = s.ReadRWString() ?? Guid.NewGuid().ToString();
                RpUserDataFormat type = (RpUserDataFormat)s.ReadInt32();
                int nelems = s.ReadInt32();
                RpUserDataArray o = new()
                {
                    Format = type,
                    Data = new DataObj[nelems]
                };
                for (int j = 0; j < nelems; ++j)
                {
                    switch (type)
                    {
                        case RpUserDataFormat.rpINTUSERDATA:
                            o.Data[j] = new() { I = s.ReadInt32()};
                            break;
                        case RpUserDataFormat.rpREALUSERDATA:
                            o.Data[j] = new() { F = s.ReadSingle() };
                            break;
                        case RpUserDataFormat.rpSTRINGUSERDATA:
                            o.Data[j] = new() { S = s.ReadRWString() };
                            break;
                    }
                }
                r[udname] = o;
            }
            return r;
        }

        internal static void Write(Dictionary<string, RpUserDataArray>  d, BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = GetSize(d),
                    Type = RwCorePluginID.USERDATAPLUGIN,
                }.Write(s);
            }
            s.Write(d.Count);
            foreach (var (k, v) in d)
            {
                if (v.Format == RpUserDataFormat.rpNAUSERDATAFORMAT)
                    throw new IOException("invalid format");
                s.WriteRWString(k);
                s.Write((int)v.Format);
                s.Write(v.Data.Length);
                foreach (var e in v.Data)
                {
                    switch (v.Format)
                    {
                        case RpUserDataFormat.rpINTUSERDATA:
                            s.Write(e.I);
                            break;
                        case RpUserDataFormat.rpREALUSERDATA:
                            s.Write(e.F);
                            break;
                        case RpUserDataFormat.rpSTRINGUSERDATA:
                            s.WriteRWString(e.S);
                            break;
                    }
                }
            }
        }
    }
    internal class RpUserDataArrayJsonConverter : JsonConverter<RpUserDataArray>
    {
        public override RpUserDataArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();
            List<RpUserDataArray.DataObj> l = [];
            RpUserDataArray.RpUserDataFormat f = RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT;
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA || f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT)
                        {
                            f = RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA;
                            l.Add(new() { S = reader.GetString()! });
                            reader.Read();
                            continue;
                        }
                        else
                        {
                            throw new JsonException("type missmatch");
                        }

                    case JsonTokenType.Number:
                        {
                            if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA)
                                throw new JsonException("type missmatch");
                            if (f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT || f == RpUserDataArray.RpUserDataFormat.rpINTUSERDATA)
                            {
                                if (reader.TryGetInt32(out int v))
                                {
                                    f = RpUserDataArray.RpUserDataFormat.rpINTUSERDATA;
                                    l.Add(new() { I = v });
                                    reader.Read();
                                    continue;
                                }
                            }
                            if (reader.TryGetSingle(out float vf))
                            {
                                if (f == RpUserDataArray.RpUserDataFormat.rpINTUSERDATA)
                                {
                                    foreach (RpUserDataArray.DataObj c in l)
                                        c.F = (float)c.I;
                                }
                                f = RpUserDataArray.RpUserDataFormat.rpREALUSERDATA;
                                l.Add(new() { F = vf });
                                reader.Read();
                                continue;
                            }

                            break;
                        }

                    case JsonTokenType.Null:
                        if (f == RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA || f == RpUserDataArray.RpUserDataFormat.rpNAUSERDATAFORMAT)
                        {
                            f = RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA;
                            l.Add(new() { });
                            reader.Read();
                            continue;
                        }
                        else
                        {
                            throw new JsonException("type missmatch");
                        }
                }
                throw new JsonException();
            }

            return new() { Format = f, Data = [.. l] };
        }

        public override void Write(Utf8JsonWriter writer, RpUserDataArray value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Data.Length; ++i)
            {
                switch (value.Format)
                {
                    case RpUserDataArray.RpUserDataFormat.rpINTUSERDATA:
                        writer.WriteNumberValue(value.Data[i].I);
                        break;
                    case RpUserDataArray.RpUserDataFormat.rpREALUSERDATA:
                        writer.WriteNumberValue(value.Data[i].F);
                        break;
                    case RpUserDataArray.RpUserDataFormat.rpSTRINGUSERDATA:
                        writer.WriteStringValue(value.Data[i].S);
                        break;
                }
            }
            writer.WriteEndArray();
        }
    }

    internal class RpHAnimHierarchy
    {
        [JsonPropertyName("nodeID")]
        [JsonInclude]
        public int NodeID;
        [JsonPropertyName("flags")]
        [JsonInclude]
        public int Flags;
        [JsonInclude]
        [JsonPropertyName("keyFrameSize")]
        public int KeyFrameSize;
        [JsonPropertyName("nodes")]
        [JsonInclude]
        public Node[] Nodes = [];
        // TODO check what parents does and where it comes from

        internal struct Node
        {
            [JsonPropertyName("flags")]
            [JsonInclude]
            public int Flags;
            [JsonPropertyName("nodeID")]
            [JsonInclude]
            public int NodeID;
            [JsonPropertyName("nodeIndex")]
            [JsonInclude]
            public int NodeIndex;

            internal const int Size = sizeof(int) * 3;
        }

        internal int Size => sizeof(int) * (3 + (Nodes.Length > 0 ? 2 : 0)) + Node.Size * Nodes.Length;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpHAnimHierarchy Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.HANIMPLUGIN);
            if (s.ReadInt32() != 256)
                throw new IOException("RpHAnimHierarchy read missing 256 constant");
            RpHAnimHierarchy r = new()
            {
                NodeID = s.ReadInt32()
            };
            int boneCount = s.ReadInt32();
            if (boneCount > 0)
            {
                r.Flags = s.ReadInt32();
                r.KeyFrameSize = s.ReadInt32();
                r.Nodes = new Node[boneCount];
                for (int i = 0; i < boneCount; ++i)
                {
                    r.Nodes[i] = new Node()
                    {
                        NodeID = s.ReadInt32(),
                        NodeIndex = s.ReadInt32(),
                        Flags = s.ReadInt32(),
                    };
                }
            }
            return r;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.HANIMPLUGIN,
                }.Write(s);
            }
            s.Write(256);
            s.Write(NodeID);
            s.Write(Nodes.Length);
            if (Nodes.Length > 0)
            {
                s.Write(Flags);
                s.Write(KeyFrameSize);
                foreach (Node n in Nodes)
                {
                    s.Write(n.NodeID);
                    s.Write(n.NodeIndex);
                    s.Write(n.Flags);
                }
            }
        }
    }

    internal class MaterialFXMaterial
    {
        internal enum DataType : int
        {
            BumpMap = 1,
            EnvMap = 2,
            DualTexture = 4,
            UVTransformMat = 5,
        };

        internal struct Data
        {
            [JsonInclude]
            public DataType Type;
            [JsonInclude]
            public Texture? Texture1;
            [JsonInclude]
            public Texture? Texture2;
            [JsonInclude]
            public float? Coefficient;
            [JsonInclude]
            public bool? FrameBufferAlpha;
            [JsonInclude]
            public int? SrcBlendMode;
            [JsonInclude]
            public int? DstBlendMode;

            internal int Size
            {
                get
                {
                    int r = sizeof(int);
                    switch (Type)
                    {
                        case DataType.BumpMap:
                            r += sizeof(float);
                            r += Texture.OptTextureSize(ref Texture1);
                            r += Texture.OptTextureSize(ref Texture2);
                            break;
                        case DataType.EnvMap:
                            r += sizeof(int) * 2; //float/int same size
                            r += Texture.OptTextureSize(ref Texture1);
                            break;
                        case DataType.DualTexture:
                            r += sizeof(int) * 2;
                            r += Texture.OptTextureSize(ref Texture1);
                            break;
                        default:
                            break;
                    }
                    return r;
                }
            }

            internal static Data Read(BinaryReader s)
            {
                Data d = new()
                {
                    Type = (DataType)s.ReadInt32()
                };
                switch (d.Type)
                {
                    case DataType.BumpMap:
                        d.Coefficient = s.ReadSingle();
                        d.Texture1 = Texture.ReadOptText(s);
                        d.Texture2 = Texture.ReadOptText(s);
                        break;
                    case DataType.EnvMap:
                        d.Coefficient = s.ReadSingle();
                        d.FrameBufferAlpha = s.ReadInt32() != 0;
                        d.Texture1 = Texture.ReadOptText(s);
                        break;
                    case DataType.DualTexture:
                        d.SrcBlendMode = s.ReadInt32();
                        d.DstBlendMode = s.ReadInt32();
                        d.Texture1 = Texture.ReadOptText(s);
                        break;
                    default:
                        break;
                }
                return d;
            }

            internal void Write(BinaryWriter s)
            {
                s.Write((int)Type);
                switch (Type)
                {
                    case DataType.BumpMap:
                        s.Write(Coefficient!.Value);
                        Texture.WriteOptTexture(s, ref Texture1);
                        Texture.WriteOptTexture(s, ref Texture2);
                        break;
                    case DataType.EnvMap:
                        s.Write(Coefficient!.Value);
                        s.Write(FrameBufferAlpha!.Value ? 1 : 0);
                        Texture.WriteOptTexture(s, ref Texture1);
                        break;
                    case DataType.DualTexture:
                        s.Write(SrcBlendMode!.Value);
                        s.Write(DstBlendMode!.Value);
                        Texture.WriteOptTexture(s, ref Texture1);
                        break;
                    default:
                        break;
                }
            }
        }

        [JsonInclude]
        internal Data Data1;
        [JsonInclude]
        internal Data Data2;
        [JsonInclude]
        internal int Flags;

        internal int Size => sizeof(int) + Data1.Size + Data2.Size;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static MaterialFXMaterial Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATERIALEFFECTSPLUGIN);
            MaterialFXMaterial r = new()
            {
                Flags = s.ReadInt32(),
                Data1 = Data.Read(s),
                Data2 = Data.Read(s)
            };

            return r;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATERIALEFFECTSPLUGIN,
                }.Write(s);
            }
            s.Write(Flags);
            Data1.Write(s);
            Data2.Write(s);
        }
    }

    internal class RpMeshHeader
    {
        [JsonInclude]
        public int Flags;
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
                Flags = s.ReadInt32(),
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

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.BINMESHPLUGIN,
                }.Write(s);
            }
            s.Write(Flags);
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
