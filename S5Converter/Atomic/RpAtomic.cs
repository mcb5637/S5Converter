﻿using S5Converter.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter.Atomic
{
    internal class RpAtomic
    {
        public struct AtomicFlagsS
        {
            [Flags]
            internal enum AtomicFlags : int
            {
                None = 0,
                CollisionTest = 1,
                RenderShadow = 2,
                Render = 4,
            }

            internal AtomicFlags Flags;

            public bool CollisionTest
            {
                readonly get => Flags.HasFlag(AtomicFlags.CollisionTest);
                set => Flags.SetFlag(value, AtomicFlags.CollisionTest);
            }
            public bool RenderShadow
            {
                readonly get => Flags.HasFlag(AtomicFlags.RenderShadow);
                set => Flags.SetFlag(value, AtomicFlags.RenderShadow);
            }
            public bool Render
            {
                readonly get => Flags.HasFlag(AtomicFlags.Render);
                set => Flags.SetFlag(value, AtomicFlags.Render);
            }
        }
        [JsonPropertyName("frameIndex")]
        public required int FrameIndex;
        [JsonPropertyName("geometryIndex")]
        public required int GeometryIndex;
        public AtomicFlagsS Flags = new() { Flags = AtomicFlagsS.AtomicFlags.None };
        public int UnknownInt1 = 0;

        [JsonPropertyName("extension")]
        public AtomicExtension Extension = new();

        internal int Size => ChunkHeader.Size + sizeof(int) * 4 + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;


        internal static RpAtomic Read(BinaryReader s, bool header, bool convertRad)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.ATOMIC);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 4 * sizeof(int))
                throw new IOException("atomic read invalid struct length");
            RpAtomic a = new()
            {
                FrameIndex = s.ReadInt32(),
                GeometryIndex = s.ReadInt32(),
                Flags = new()
                {
                    Flags = (AtomicFlagsS.AtomicFlags)s.ReadInt32(),
                },
                UnknownInt1 = s.ReadInt32()
            };
            a.Extension.ConvertRadians = convertRad;
            a.Extension.Read(s, a);
            return a;
        }

        internal void Write(BinaryWriter s, bool header, bool convertRad, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.ATOMIC,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 4 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(FrameIndex);
            s.Write(GeometryIndex);
            s.Write((int)Flags.Flags);
            s.Write(UnknownInt1);
            Extension.ConvertRadians = convertRad;
            Extension.Write(s, this, versionNum, buildNum);
        }
    }

    internal class AtomicExtension : Extension<RpAtomic>
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? MaterialFXAtomic_EffectsEnabled = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParticleStandard? ParticleStandard = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RightToRender? RightToRender = null;

        internal bool ConvertRadians = true;

        internal override int Size(RpAtomic obj)
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

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RpAtomic obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.MATERIALEFFECTSPLUGIN:
                    MaterialFXAtomic_EffectsEnabled = s.ReadInt32() != 0;
                    break;
                case RwCorePluginID.PRTSTDPLUGIN:
                    ParticleStandard = ParticleStandard.Read(s, false, ConvertRadians);
                    break;
                case RwCorePluginID.RIGHTTORENDER:
                    RightToRender = RightToRender.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, RpAtomic obj, uint versionNum, uint buildNum)
        {
            RightToRender?.Write(s, true, versionNum, buildNum);
            if (MaterialFXAtomic_EffectsEnabled != null)
            {
                new ChunkHeader()
                {
                    Length = sizeof(int),
                    Type = RwCorePluginID.MATERIALEFFECTSPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
                s.Write(MaterialFXAtomic_EffectsEnabled.Value ? 1 : 0);
            }
            ParticleStandard?.Write(s, true, ConvertRadians, versionNum, buildNum);
        }
    }
}
