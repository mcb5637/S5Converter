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

        internal static RWFile Read(BinaryReader s)
        {
            ChunkHeader h = ChunkHeader.Read(s);
            RWFile f = new();
            switch(h.Type)
            {
                case RwCorePluginID.CLUMP:
                    f.Clp = Clump.Read(s);
                    break;
                default:
                    Console.Error.WriteLine($"invalid top level type {h.Type}");
                    break;
            }
            return f;
        }
    }
}
