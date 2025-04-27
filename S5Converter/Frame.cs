using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Frame : IJsonOnDeserialized
    {
        [JsonPropertyName("parentFrameIndex")]
        [JsonInclude]
        public int ParentFrameIndex;
        [JsonPropertyName("position")]
        [JsonInclude]
        public Vec3 Position;
        [JsonPropertyName("rotationMatrix")]
        [JsonInclude]
        public Vec3[] RotationMatrix = new Vec3[3];
        internal ref Vec3 Right => ref RotationMatrix[0];
        internal ref Vec3 Up => ref RotationMatrix[1];
        internal ref Vec3 At => ref RotationMatrix[2];

        [JsonInclude]
        public int UnknownIntProbablyUnused;

        internal const int Size = Vec3.Size * 4 + sizeof(int) * 2;

        internal static Frame Read(BinaryReader s)
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

        public void OnDeserialized()
        {
            RotationMatrix ??= new Vec3[3];
        }
    }
    internal struct Vec2
    {
        [JsonPropertyName("x")]
        [JsonInclude]
        public float X;
        [JsonPropertyName("y")]
        [JsonInclude]
        public float Y;

        internal const int Size = 2 * sizeof(float);

        internal static Vec2 Read(BinaryReader s)
        {
            return new()
            {
                X = s.ReadSingle(),
                Y = s.ReadSingle(),
            };
        }
        internal readonly void Write(BinaryWriter s)
        {
            s.Write(X);
            s.Write(Y);
        }
    }
    internal struct Vec3
    {
        [JsonPropertyName("x")]
        [JsonInclude]
        public float X;
        [JsonPropertyName("y")]
        [JsonInclude]
        public float Y;
        [JsonPropertyName("z")]
        [JsonInclude]
        public float Z;

        internal const int Size = 3 * sizeof(float);

        internal static Vec3 Read(BinaryReader s)
        {
            return new()
            {
                X = s.ReadSingle(),
                Y = s.ReadSingle(),
                Z = s.ReadSingle(),
            };
        }
        internal readonly void Write(BinaryWriter s)
        {
            s.Write(X);
            s.Write(Y);
            s.Write(Z);
        }
    }
    internal struct Sphere
    {
        [JsonIgnore]
        public Vec3 Center;
        [JsonPropertyName("x")]
        public float X { readonly get => Center.X; set => Center.X = value; }
        [JsonPropertyName("y")]
        public float Y { readonly get => Center.Y; set => Center.Y = value; }
        [JsonPropertyName("z")]
        public float Z { readonly get => Center.Z; set => Center.Z = value; }

        [JsonPropertyName("radius")]
        [JsonInclude]
        public float Radius;

        internal const int Size = Vec3.Size + sizeof(float);

        internal static Sphere Read(BinaryReader s)
        {
            return new()
            {
                Center = Vec3.Read(s),
                Radius = s.ReadSingle(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            Center.Write(s);
            s.Write(Radius);
        }
    }

    internal struct RwMatrix
    {
        [JsonInclude]
        public Vec3 Right;
        [JsonInclude]
        public Vec3 Up;
        [JsonInclude]
        public Vec3 At;
        [JsonInclude]
        public Vec3 Pos;
        [JsonInclude]
        public int Flags;

        private const int SizeData = Vec3.Size * 4 + sizeof(int);
        internal const int Size = SizeData + ChunkHeader.Size;
        internal const int SizeH = Size + ChunkHeader.Size;

        internal static RwMatrix Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATRIX);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != SizeData)
                throw new IOException("matrix invalid struct size");
            return new()
            {
                Right = Vec3.Read(s),
                Up = Vec3.Read(s),
                At = Vec3.Read(s),
                Pos = Vec3.Read(s),
                Flags = s.ReadInt32(),
            };
        }

        internal readonly void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATRIX,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = SizeData,
                Type = RwCorePluginID.STRUCT,
            }.Write(s);
            Right.Write(s);
            Up.Write(s);
            At.Write(s);
            Pos.Write(s);
            s.Write(Flags);
        }
    }

    internal class FrameWithExt : IJsonOnDeserialized
    {
        [JsonPropertyName("frame")]
        [JsonInclude]
        public Frame Frame = new();
        [JsonPropertyName("extension")]
        [JsonInclude]
        public FrameExtension Extension = new();

        public void OnDeserialized()
        {
            Frame ??= new();
            Extension ??= new();
        }
    }

    internal class FrameExtension : Extension<Frame>
    {
        [JsonPropertyName("userDataPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, RpUserDataArray>? UserDataPLG;

        [JsonPropertyName("hanimPLG")]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RpHAnimHierarchy? HanimPLG;

        internal override int Size(Frame obj)
        {
            int r = 0;
            if (UserDataPLG != null)
                r += RpUserDataArray.GetSizeH(UserDataPLG);
            if (HanimPLG != null)
                r += HanimPLG.SizeH;
            return r;
        }

        internal override bool TryRead(BinaryReader s, ref ChunkHeader h, Frame obj)
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

        internal override void WriteExt(BinaryWriter s, Frame obj)
        {
            if (UserDataPLG != null)
                RpUserDataArray.Write(UserDataPLG, s, true);
            HanimPLG?.Write(s, true);
        }
    }
}
