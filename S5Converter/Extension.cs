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

        internal static Extension Read(BinaryReader s)
        {
            ChunkHeader exheader = ChunkHeader.FindChunk(s, RwCorePluginID.EXTENSION);
            Extension e = new();
            while (exheader.Length > 0)
            {
                ChunkHeader h = ChunkHeader.Read(s);
                switch (h.Type)
                {
                    case RwCorePluginID.UserData:
                        e.UserDataPLG = ReadUserData(s);
                        break;
                    case RwCorePluginID.HAnim:
                        e.HanimPLG = RpHAnimHierarchy.Read(s);
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
}
