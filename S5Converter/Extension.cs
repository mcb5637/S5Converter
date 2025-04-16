using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static S5Converter.RpUserDataArray;

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
                        e.UserDataPLG = RpUserDataArray.Read(s);
                        break;
                    case (RwCorePluginID.HANIMPLUGIN, RwCorePluginID.FRAMELIST):
                        e.HanimPLG = RpHAnimHierarchy.Read(s);
                        break;
                    case (RwCorePluginID.MATERIALEFFECTSPLUGIN, RwCorePluginID.MATERIAL):
                        e.MaterialFXMat = MaterialFXMaterial.Read(s);
                        break;
                    case (RwCorePluginID.MATERIALEFFECTSPLUGIN, RwCorePluginID.ATOMIC):
                        e.MaterialFXAtomic_EffectsEnabled = s.ReadInt32() != 0;
                        break;
                    case (RwCorePluginID.BINMESHPLUGIN, RwCorePluginID.GEOMETRY):
                        e.BinMeshPLG = RpMeshHeader.Read(s);
                        break;
                    default:
                        Console.Error.WriteLine($"unknown extension {(int)h.Type}, skipping");
                        s.ReadBytes((int)h.Length);
                        break;
                }
                exheader.Length -= h.Length + 12;
            }
            return e;
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
        }

        public RpUserDataFormat Format;
        public DataObj[] Data = [];


        internal static Dictionary<string, RpUserDataArray> Read(BinaryReader s)
        {
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
        }

        internal static RpHAnimHierarchy Read(BinaryReader s)
        {
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
        }

        [JsonInclude]
        internal Data Data1;
        [JsonInclude]
        internal Data Data2;
        [JsonInclude]
        internal int Flags;

        internal static MaterialFXMaterial Read(BinaryReader s)
        {
            MaterialFXMaterial r = new()
            {
                Flags = s.ReadInt32(),
            };
            ReadData(ref r.Data1, s);
            ReadData(ref r.Data2, s);

            return r;
        }

        private static void ReadData(ref Data d, BinaryReader s)
        {
            d.Type = (DataType)s.ReadInt32();
            switch (d.Type)
            {
                case DataType.BumpMap:
                    d.Coefficient = s.ReadSingle();
                    d.Texture1 = ReadOptText(s);
                    d.Texture2 = ReadOptText(s);
                    break;
                case DataType.EnvMap:
                    d.Coefficient = s.ReadSingle();
                    d.FrameBufferAlpha = s.ReadInt32() != 0;
                    d.Texture1 = ReadOptText(s);
                    break;
                case DataType.DualTexture:
                    d.SrcBlendMode = s.ReadInt32();
                    d.DstBlendMode = s.ReadInt32();
                    d.Texture1 = ReadOptText(s);
                    break;
                default:
                    break;
            }
        }

        private static Texture? ReadOptText(BinaryReader s)
        {
            if (s.ReadInt32() == 0)
            {
                return null;
            }
            else
            {
                ChunkHeader.FindChunk(s, RwCorePluginID.TEXTURE);
                return Texture.Read(s);
            }
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
        }

        internal static RpMeshHeader Read(BinaryReader s)
        {
            RpMeshHeader r = new()
            {
                Flags = s.ReadInt32(),
            };
            int numMeshes = s.ReadInt32();
            int totalInices = s.ReadInt32();

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
    }
}
