using System;
using System.Collections.Generic;
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
        public Dictionary<string, string?[]>? UserDataPLG; // TODO better representation?

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

        internal static Extension Read(BinaryReader s, RwCorePluginID src)
        {
            ChunkHeader exheader = ChunkHeader.FindChunk(s, RwCorePluginID.EXTENSION);
            Extension e = new();
            while (exheader.Length > 0)
            {
                ChunkHeader h = ChunkHeader.Read(s);
                switch ((h.Type, src))
                {
                    case (RwCorePluginID.UserData, RwCorePluginID.FRAMELIST):
                        e.UserDataPLG = ReadUserData(s);
                        break;
                    case (RwCorePluginID.HAnim, RwCorePluginID.FRAMELIST):
                        e.HanimPLG = RpHAnimHierarchy.Read(s);
                        break;
                    case (RwCorePluginID.MaterialFX, RwCorePluginID.MATERIAL):
                        e.MaterialFXMat = MaterialFXMaterial.Read(s);
                        break;
                    case (RwCorePluginID.MaterialFX, RwCorePluginID.ATOMIC):
                        e.MaterialFXAtomic_EffectsEnabled = s.ReadInt32() != 0;
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

        private static Dictionary<string, string?[]> ReadUserData(BinaryReader s)
        {
            Dictionary<string, string?[]> r = [];
            int numUD = s.ReadInt32();
            for (int i = 0; i < numUD; ++i)
            {
                string udname = s.ReadRWString() ?? Guid.NewGuid().ToString();
                int type = s.ReadInt32();
                int nelems = s.ReadInt32();
                string?[] o = new string?[nelems];
                for (int j = 0; j < nelems; ++j)
                {
                    switch (type)
                    {
                        case 1: // int
                            o[j] = s.ReadInt32().ToString();
                            break;
                        case 2: // float
                            o[j] = s.ReadSingle().ToString();
                            break;
                        case 3: // string
                            o[j] = s.ReadRWString();
                            break;
                    }
                }
                r[udname] = o;
            }
            return r;
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
        [JsonIgnore]
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
}
