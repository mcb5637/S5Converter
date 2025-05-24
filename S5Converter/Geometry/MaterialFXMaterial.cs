using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class MaterialFXMaterial
    {
        [JsonConverter(typeof(EnumJsonConverter<DataType>))]
        internal enum DataType : int
        {
            None = 0,
            BumpMap = 1,
            EnvMap = 2,
            DualTexture = 4,
            UVTransformMat = 5,
        };
        [JsonConverter(typeof(EnumJsonConverter<RpMatFXMaterialFlags>))]
        internal enum RpMatFXMaterialFlags : int
        {
            None = 0,
            BumpMap = 1,
            EnvMap = 2,
            BumpEnvMap = 3,
            DualTexture = 4,
            UVTransform = 5,
            DualTextureUVTransform = 6,
        };

        internal struct Data
        {
            public DataType Type;
            public RwTexture? Texture1;
            public RwTexture? Texture2;
            public float? Coefficient;
            public bool? FrameBufferAlpha;
            public RwBlendFunction? SrcBlendMode;
            public RwBlendFunction? DstBlendMode;

            internal readonly int Size
            {
                get
                {
                    int r = sizeof(int);
                    switch (Type)
                    {
                        case DataType.BumpMap:
                            r += sizeof(float);
                            r += RwTexture.OptTextureSize(Texture1);
                            r += RwTexture.OptTextureSize(Texture2);
                            break;
                        case DataType.EnvMap:
                            r += sizeof(int) * 2; //float/int same size
                            r += RwTexture.OptTextureSize(Texture1);
                            break;
                        case DataType.DualTexture:
                            r += sizeof(int) * 2;
                            r += RwTexture.OptTextureSize(Texture1);
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
                        d.Texture1 = RwTexture.ReadOptText(s);
                        d.Texture2 = RwTexture.ReadOptText(s);
                        break;
                    case DataType.EnvMap:
                        d.Coefficient = s.ReadSingle();
                        d.FrameBufferAlpha = s.ReadInt32() != 0;
                        d.Texture1 = RwTexture.ReadOptText(s);
                        break;
                    case DataType.DualTexture:
                        d.SrcBlendMode = (RwBlendFunction)s.ReadInt32();
                        d.DstBlendMode = (RwBlendFunction)s.ReadInt32();
                        d.Texture1 = RwTexture.ReadOptText(s);
                        break;
                    default:
                        break;
                }
                return d;
            }

            internal void Write(BinaryWriter s, uint versionNum, uint buildNum)
            {
                s.Write((int)Type);
                switch (Type)
                {
                    case DataType.BumpMap:
                        s.Write(Coefficient!.Value);
                        RwTexture.WriteOptTexture(s, ref Texture1, versionNum, buildNum);
                        RwTexture.WriteOptTexture(s, ref Texture2, versionNum, buildNum);
                        break;
                    case DataType.EnvMap:
                        s.Write(Coefficient!.Value);
                        s.Write(FrameBufferAlpha!.Value ? 1 : 0);
                        RwTexture.WriteOptTexture(s, ref Texture1, versionNum, buildNum);
                        break;
                    case DataType.DualTexture:
                        s.Write((int)SrcBlendMode!.Value);
                        s.Write((int)DstBlendMode!.Value);
                        RwTexture.WriteOptTexture(s, ref Texture1, versionNum, buildNum);
                        break;
                    default:
                        break;
                }
            }
        }

        public Data Data1;
        public Data Data2;
        public RpMatFXMaterialFlags Flags;

        internal int Size => sizeof(int) + Data1.Size + Data2.Size;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static MaterialFXMaterial Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATERIALEFFECTSPLUGIN);
            MaterialFXMaterial r = new()
            {
                Flags = (RpMatFXMaterialFlags)s.ReadInt32(),
                Data1 = Data.Read(s),
                Data2 = Data.Read(s)
            };

            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATERIALEFFECTSPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            switch (Flags)
            {
                case RpMatFXMaterialFlags.None:
                    if (Data1.Type != DataType.None || Data2.Type != DataType.None)
                        throw new IOException("materialeffects None data type mismatch");
                    break;
                case RpMatFXMaterialFlags.BumpMap:
                    if (Data1.Type != DataType.BumpMap || Data2.Type != DataType.None)
                        throw new IOException("materialeffects BumpMap data type mismatch");
                    break;
                case RpMatFXMaterialFlags.EnvMap:
                    if (Data1.Type != DataType.EnvMap || Data2.Type != DataType.None)
                        throw new IOException("materialeffects EnvMap data type mismatch");
                    break;
                case RpMatFXMaterialFlags.BumpEnvMap:
                    if (Data1.Type != DataType.BumpMap || Data2.Type != DataType.EnvMap)
                        throw new IOException("materialeffects BumpEnvMap data type mismatch");
                    break;
                case RpMatFXMaterialFlags.DualTexture:
                    if (Data1.Type != DataType.DualTexture || Data2.Type != DataType.None)
                        throw new IOException("materialeffects DualTexture data type mismatch");
                    break;
                case RpMatFXMaterialFlags.UVTransform:
                    if (Data1.Type != DataType.UVTransformMat || Data2.Type != DataType.None)
                        throw new IOException("materialeffects UVTransform data type mismatch");
                    break;
                case RpMatFXMaterialFlags.DualTextureUVTransform:
                    if (Data1.Type != DataType.UVTransformMat || Data2.Type != DataType.DualTexture)
                        throw new IOException("materialeffects DualTextureUVTransform data type mismatch");
                    break;
            }
            s.Write((int)Flags);
            Data1.Write(s, versionNum, buildNum);
            Data2.Write(s, versionNum, buildNum);
        }
    }
}
