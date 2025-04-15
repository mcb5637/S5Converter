using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class Frame
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

        internal static Vec3 Read(BinaryReader s)
        {
            return new()
            {
                X = s.ReadSingle(),
                Y = s.ReadSingle(),
                Z = s.ReadSingle(),
            };
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

        internal static Sphere Read(BinaryReader s)
        {
            return new()
            {
                Center = Vec3.Read(s),
                Radius = s.ReadSingle(),
            };
        }
    }
    internal class FrameWithExt
    {
        [JsonPropertyName("frame")]
        [JsonInclude]
        public Frame Frame = new();
        [JsonPropertyName("extension")]
        [JsonInclude]
        public Extension Extension = new();
    }
}
