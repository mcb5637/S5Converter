using S5Converter.CommonExtensions;
using S5Converter.Frame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class RpLight
    {
        [JsonConverter(typeof(EnumJsonConverter<RpLightType>))]
        internal enum RpLightType : int
        {
            rpNALIGHTTYPE = 0,

		    rpLIGHTDIRECTIONAL,
		    rpLIGHTAMBIENT,

		    rpLIGHTPOINT = 0x80,
		    rpLIGHTSPOT,
		    rpLIGHTSPOTSOFT,
	    };

        public required float Radius;
        public required RGBAF Color;
        public required float MinusCosAngle;
        public required RpLightType Type;

        [JsonPropertyName("extension")]
        public LightExtension Extension = new();

        internal int Size => ChunkHeader.Size + 6 * sizeof(float) + Extension.SizeH(this);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpLight Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.LIGHT);
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            RpLight r = new() {
                Radius = s.ReadSingle(),
                Color = new()
                {
                    Red = s.ReadSingle(),
                    Green = s.ReadSingle(),
                    Blue = s.ReadSingle(),
                    Alpha = 1.0f,
                },
                MinusCosAngle = s.ReadSingle(),
                Type = (RpLightType)s.ReadInt32(),
            };
            r.Extension.Read(s, r);
            return r;
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    BuildNum = buildNum,
                    Version = versionNum,
                    Length = Size,
                    Type = RwCorePluginID.LIGHT,
                }.Write(s);
            }
            new ChunkHeader()
            {
                BuildNum = buildNum,
                Version = versionNum,
                Length = sizeof(float) * 6,
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(Radius);
            s.Write(Color.Red);
            s.Write(Color.Green);
            s.Write(Color.Blue);
            s.Write(MinusCosAngle);
            s.Write((int)Type);

            Extension.Write(s, this, versionNum, buildNum);
        }
    }

    internal class RpLight_WithFrameIndex
    {
        public required int FrameIndex;
        public required RpLight Light;

        internal int Size => Light.Size + sizeof(int) + ChunkHeader.Size;

        internal static RpLight_WithFrameIndex Read(BinaryReader s)
        {
            ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT);
            RpLight_WithFrameIndex r = new() {
                FrameIndex = s.ReadInt32(),
                Light = RpLight.Read(s, true),
            };
            return r;
        }

        internal void Write(BinaryWriter s, UInt32 versionNum, UInt32 buildNum)
        {
            new ChunkHeader()
            {
                BuildNum = buildNum,
                Version = versionNum,
                Length = sizeof(int),
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            s.Write(FrameIndex);
            Light.Write(s, true, versionNum, buildNum);
        }
    }

    internal class LightExtension : Extension<RpLight>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG = null;

        internal override int Size(RpLight obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RpLight obj)
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

        internal override void WriteExt(BinaryWriter s, RpLight obj, UInt32 versionNum, UInt32 buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
        }
    }
}
