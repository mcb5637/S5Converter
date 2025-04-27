using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Atomic : IJsonOnDeserialized
    {
        [JsonPropertyName("frameIndex")]
        [JsonInclude]
        public int FrameIndex;
        [JsonPropertyName("geometryIndex")]
        [JsonInclude]
        public int GeometryIndex;
        [JsonInclude]
        public int Flags;
        [JsonInclude]
        public int UnknownInt1;

        [JsonPropertyName("extension")]
        [JsonInclude]
        public AtomicExtension Extension = new();

        internal int Size => ChunkHeader.Size + sizeof(int) * 4 + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;


        internal static Atomic Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.ATOMIC);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 4 * sizeof(int))
                throw new IOException("atomic read invalid struct length");
            Atomic a = new()
            {
                FrameIndex = s.ReadInt32(),
                GeometryIndex = s.ReadInt32(),
                Flags = s.ReadInt32(),
                UnknownInt1 = s.ReadInt32(),
            };
            a.Extension.Read(s, a);
            return a;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.ATOMIC,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 4 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(FrameIndex);
            s.Write(GeometryIndex);
            s.Write(Flags);
            s.Write(UnknownInt1);
            Extension.Write(s, this);
        }
        public void OnDeserialized()
        {
            Extension ??= new();
        }
    }

    internal class AtomicExtension : Extension<Atomic>
    {
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? MaterialFXAtomic_EffectsEnabled;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParticleStandard? ParticleStandard;

        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RightToRender? RightToRender;

        internal override int Size(Atomic obj)
        {
            int r = 0;
            if (MaterialFXAtomic_EffectsEnabled != null)
                r += sizeof(int) + ChunkHeader.Size;
            if (ParticleStandard != null)
                r += ParticleStandard.SizeH;
            if (RightToRender != null)
                r += RightToRender.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Atomic obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.MATERIALEFFECTSPLUGIN:
                    MaterialFXAtomic_EffectsEnabled = s.ReadInt32() != 0;
                    break;
                case RwCorePluginID.PRTSTDPLUGIN:
                    ParticleStandard = ParticleStandard.Read(s, false);
                    break;
                case RwCorePluginID.RIGHTTORENDER:
                    RightToRender = RightToRender.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, Atomic obj)
        {
            if (MaterialFXAtomic_EffectsEnabled != null)
            {
                new ChunkHeader()
                {
                    Length = sizeof(int),
                    Type = RwCorePluginID.MATERIALEFFECTSPLUGIN,
                }.Write(s);
                s.Write(MaterialFXAtomic_EffectsEnabled.Value ? 1 : 0);
            }
            ParticleStandard?.Write(s, true);
            RightToRender?.Write(s, true);
        }
    }
}
