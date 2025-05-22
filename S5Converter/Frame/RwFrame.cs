using S5Converter.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter.Frame
{
    internal class RwFrame
    {
        [JsonPropertyName("parentFrameIndex")]
        public required int ParentFrameIndex;
        [JsonPropertyName("position")]
        public required Vec3 Position;
        [JsonPropertyName("rotationMatrix")]
        [JsonRequired]
        public Vec3[] RotationMatrix = new Vec3[3];
        internal ref Vec3 Right => ref RotationMatrix[0];
        internal ref Vec3 Up => ref RotationMatrix[1];
        internal ref Vec3 At => ref RotationMatrix[2];

        public int UnknownIntProbablyUnused = 0;

        internal const int Size = Vec3.Size * 4 + sizeof(int) * 2;

        internal static RwFrame Read(BinaryReader s)
        {
            return new()
            {
                Right = Vec3.Read(s),
                Up = Vec3.Read(s),
                At = Vec3.Read(s),
                Position = Vec3.Read(s),
                ParentFrameIndex = s.ReadInt32(),
                UnknownIntProbablyUnused = s.ReadInt32()
            };
        }

        internal void Write(BinaryWriter s)
        {
            Right.Write(s);
            Up.Write(s);
            At.Write(s);
            Position.Write(s);
            s.Write(ParentFrameIndex);
            s.Write(UnknownIntProbablyUnused);
        }
    }

    internal class FrameWithExt
    {
        [JsonPropertyName("frame")]
        public required RwFrame Frame;
        [JsonPropertyName("extension")]
        public FrameExtension Extension = new();
    }

    internal class FrameExtension : Extension<RwFrame>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG = null;

        [JsonPropertyName("hanimPLG")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpHAnimHierarchy? HanimPLG = null;

        internal override int Size(RwFrame obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (HanimPLG != null)
                r += HanimPLG.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, RwFrame obj)
        {
            switch (h.Type)
            {
                case RwCorePluginID.USERDATAPLUGIN:
                    UserDataPLG = RpUserDataArray.Read(s, false);
                    break;
                case RwCorePluginID.HANIMPLUGIN:
                    HanimPLG = RpHAnimHierarchy.Read(s, false);
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal override void WriteExt(BinaryWriter s, RwFrame obj, uint versionNum, uint buildNum)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true, versionNum, buildNum);
            HanimPLG?.Write(s, true, versionNum, buildNum);
        }
    }
}
