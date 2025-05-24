using S5Converter.CommonExtensions;
using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class RwTexture
    {
        [JsonConverter(typeof(EnumJsonConverter<RwTextureFilterMode>))]
        internal enum RwTextureFilterMode : uint
        {
            None = 0,
            Nearest_NoMipMap,
            Linear_NoMipMap,
            Nearest_MipMap,
            Linear_MipMap,
            Linear_MipMap_Nearest,
            Linear_MipMap_Linear,
        };
        [JsonConverter(typeof(EnumJsonConverter<RwTextureAddressMode>))]
        internal enum RwTextureAddressMode : uint
        {
            None = 0,
            Wrap,
            WrapMirror,
            Clamp,
            Border,
        };
        internal struct TextureFlagsS
        {
            internal ushort Flags;

            internal const uint rwTEXTUREFILTERMODEMASK = 0x000000FF;
            internal const uint rwTEXTUREADDRESSINGUMASK = 0x00000F00;
            internal const uint rwTEXTUREADDRESSINGVMASK = 0x0000F000;

            private uint F
            {
                readonly get => Flags;
                set => Flags = (ushort)value;
            }

            public RwTextureFilterMode FilterMode {
                readonly get => (RwTextureFilterMode)(F & rwTEXTUREFILTERMODEMASK);
                set {
                    uint f = F & ~rwTEXTUREFILTERMODEMASK;
                    F = f | (uint)value;
                }
            }

            public RwTextureAddressMode AddressModeU
            {
                readonly get => (RwTextureAddressMode)((F & rwTEXTUREADDRESSINGUMASK) >> 8);
                set
                {
                    uint f = F & ~rwTEXTUREADDRESSINGUMASK;
                    F = f | (uint)value << 8;
                }
            }
            public RwTextureAddressMode AddressModeV
            {
                readonly get => (RwTextureAddressMode)((F & rwTEXTUREADDRESSINGVMASK) >> 12);
                set
                {
                    uint f = F & ~rwTEXTUREADDRESSINGVMASK;
                    F = f | (uint)value << 12;
                }
            }
        }


        [JsonPropertyName("texture")]
        public required string Tex;
        public int[]? TexPadding = null;
        [JsonPropertyName("textureAlpha")]
        public string TextureAlpha = "";
        public int[]? TextureAlphaPadding = null;
        public TextureFlagsS FilterAddressing = new()
        {
            Flags = 0,
        };
        public short UnusedInt1 = 0;

        [JsonPropertyName("extension")]
        public TextureExtension Extension = new();

        public RwTexture() { }

        internal int Size => ChunkHeader.Size + ChunkHeader.GetStringSize(Tex) + ChunkHeader.GetStringSize(TextureAlpha)
            + 2 * sizeof(short) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RwTexture Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.TEXTURE);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 2 * sizeof(short))
                throw new IOException("Texture struct invalid length");
            RwTexture r = new()
            {
                Tex = "",
                FilterAddressing = new() {
                    Flags = s.ReadUInt16(),
                },
                UnusedInt1 = s.ReadInt16(),
            };
            (r.Tex, r.TexPadding) = ChunkHeader.FindAndReadString(s);
            (r.TextureAlpha, r.TextureAlphaPadding) = ChunkHeader.FindAndReadString(s);
            r.Extension.Read(s, r);

            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (ChunkHeader.GetStringSize(Tex) > 0x80)
                throw new IOException("Texture name too long");
            if (ChunkHeader.GetStringSize(TextureAlpha) > 0x80)
                throw new IOException("Texture name too long");
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.TEXTURE,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 2 * sizeof(short),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(FilterAddressing.Flags);
            s.Write(UnusedInt1);
            ChunkHeader.WriteString(s, Tex, TexPadding, versionNum, buildNum);
            ChunkHeader.WriteString(s, TextureAlpha, TextureAlphaPadding, versionNum, buildNum);
            Extension.Write(s, this, versionNum, buildNum);
        }

        internal static int OptTextureSize(RwTexture? t)
        {
            if (t == null)
            {
                return sizeof(int);
            }
            else
            {
                return sizeof(int) + t.SizeH;
            }
        }
        internal static RwTexture? ReadOptText(BinaryReader s)
        {
            if (s.ReadInt32() == 0)
            {
                return null;
            }
            else
            {
                return Read(s, true);
            }
        }
        internal static void WriteOptTexture(BinaryWriter s, ref RwTexture? t, uint versionNum, uint buildNum)
        {
            if (t == null)
            {
                s.Write(0);
            }
            else
            {
                s.Write(1);
                t.Write(s, true, versionNum, buildNum);
            }
        }
    }

    internal class TextureExtension : Extension<RwTexture>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        internal override int Size(RwTexture obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RwTexture obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, RwTexture obj, uint versionNum, uint buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
        }
    }
}
