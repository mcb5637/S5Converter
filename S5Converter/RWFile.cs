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
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RtCompressedAnim? CompressedAnim;
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpHierarchicalAnim? HierarchicalAnim;
        [JsonInclude]
        public UInt32 BuildNum = ChunkHeader.DefaultBuildNum;
        [JsonInclude]
        public UInt32 VersionNum = ChunkHeader.rwLIBRARYCURRENTVERSION;


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
                case RwCorePluginID.ANIMANIMATION:
                    RtAnimAnimation a = RtAnimAnimation.ReadAnyAnim(s, false);
                    if (a is RtCompressedAnim ca)
                        f.CompressedAnim = ca;
                    else if (a is RpHierarchicalAnim ha)
                        f.HierarchicalAnim = ha;
                    else
                        throw new IOException($"invalid anim type");
                    break;
                default:
                    throw new IOException($"invalid top level type {h.Type}");
            }
            f.BuildNum = h.BuildNum;
            f.VersionNum = h.Version;
            return f;
        }

        internal void Write(BinaryWriter s)
        {
            if (new object?[] { Clp, UVAnimDict, CompressedAnim, HierarchicalAnim }.Count(x => x != null) != 1)
                throw new IOException("file: not exactly 1 member set");
            if (Clp != null)
            {
                Clp.Write(s, true, VersionNum, BuildNum);
                return;
            }
            if (UVAnimDict != null)
            {
                RwDict.Write(UVAnimDict, s, true, VersionNum, BuildNum);
                return;
            }
            if (CompressedAnim != null)
            {
                CompressedAnim.Write(s, true, VersionNum, BuildNum);
                return;
            }
            if (HierarchicalAnim != null)
            {
                HierarchicalAnim.Write(s, true, VersionNum, BuildNum);
                return;
            }
            throw new IOException("empty");
        }
    }
}
