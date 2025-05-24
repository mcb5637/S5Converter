using S5Converter.CommonExtensions;
using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal class RpMaterial
    {
        public int UnknownInt1;
        [JsonPropertyName("color")]
        public RGBA Color;
        public int UnknownInt2;
        public RwSurfaceProperties SurfaceProps;
        [JsonPropertyName("textures")]
        public RwTexture[] Textures = [];

        [JsonPropertyName("extension")]
        public MaterialExtension Extension = new();

        internal int Size => ChunkHeader.Size + RGBA.Size + sizeof(int) * 3 + RwSurfaceProperties.Size + Textures.Sum(t => t.SizeH) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpMaterial Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATERIAL);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != 7 * sizeof(int))
                throw new IOException("Material struct invalid length");
            RpMaterial m = new()
            {
                UnknownInt1 = s.ReadInt32(),
                Color = RGBA.Read(s),
                UnknownInt2 = s.ReadInt32(),
            };
            bool hastex = s.ReadInt32() != 0;
            m.SurfaceProps.Ambient = s.ReadSingle();
            m.SurfaceProps.Specular = s.ReadSingle();
            m.SurfaceProps.Diffuse = s.ReadSingle();
            if (hastex)
            {
                m.Textures = [RwTexture.Read(s, true)];
            }

            m.Extension.Read(s, m);

            return m;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATERIAL,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = 7 * sizeof(int),
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(UnknownInt1);
            Color.Write(s);
            s.Write(UnknownInt2);
            s.Write(Textures.Length > 0 ? 1 : 0);
            s.Write(SurfaceProps.Ambient);
            s.Write(SurfaceProps.Specular);
            s.Write(SurfaceProps.Diffuse);
            foreach (RwTexture t in Textures)
                t.Write(s, true, versionNum, buildNum);

            Extension.Write(s, this, versionNum, buildNum);
        }
    }

    internal class MaterialExtension : Extension<RpMaterial>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaterialFXMaterial? MaterialFXMat;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RightToRender? RightToRender;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaterialUVAnim? MaterialUVAnim;

        internal override int Size(RpMaterial obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (MaterialFXMat != null)
                r += MaterialFXMat.SizeH;
            if (RightToRender != null)
                r += RightToRender.SizeH;
            if (MaterialUVAnim != null)
                r += MaterialUVAnim.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RpMaterial obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                case RwCorePluginID.MATERIALEFFECTSPLUGIN:
                    MaterialFXMat = MaterialFXMaterial.Read(s, false);
                    break;
                case RwCorePluginID.RIGHTTORENDER:
                    RightToRender = RightToRender.Read(s, false);
                    break;
                case RwCorePluginID.UVANIMPLUGIN:
                    MaterialUVAnim = MaterialUVAnim.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, RpMaterial obj, uint versionNum, uint buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
            MaterialFXMat?.Write(s, true, versionNum, buildNum);
            RightToRender?.Write(s, true, versionNum, buildNum);
            MaterialUVAnim?.Write(s, true, versionNum, buildNum);
        }
    }
}
