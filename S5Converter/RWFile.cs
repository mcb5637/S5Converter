using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class RWFile
    {
        [JsonPropertyName("clump")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Clump? Clp;
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpUVAnim[]? UVAnimDict;


        internal static RWFile Read(BinaryReader s)
        {
            ChunkHeader h = ChunkHeader.Read(s);
            RWFile f = new();
            switch(h.Type)
            {
                case RwCorePluginID.CLUMP:
                    f.Clp = Clump.Read(s, false);
                    break;
                case RwCorePluginID.UVANIMDICT:
                    f.UVAnimDict = RwDict.Read<RpUVAnim>(s, false);
                    break;
                default:
                    throw new IOException($"invalid top level type {h.Type}");
            }
            return f;
        }

        internal void Write(BinaryWriter s)
        {
            if (new object?[] { Clp, UVAnimDict }.Count(x => x != null) != 1)
                throw new IOException("file: not exactly 1 member set");
            if (Clp != null)
            {
                Clp.Write(s, true);
                return;
            }
            if (UVAnimDict != null)
            {
                RwDict.Write(UVAnimDict, s, true);
                return;
            }
            throw new IOException("empty");
        }
    }
}
