using System.Text.Json.Serialization;

namespace S5Converter
{
    internal struct RwSphere
    {
        internal Vec3 Center;
        [JsonPropertyName("x")]
        [JsonRequired]
        public float X { readonly get => Center.X; set => Center.X = value; }
        [JsonPropertyName("y")]
        [JsonRequired]
        public float Y { readonly get => Center.Y; set => Center.Y = value; }
        [JsonPropertyName("z")]
        [JsonRequired]
        public float Z { readonly get => Center.Z; set => Center.Z = value; }

        [JsonPropertyName("radius")]
        [JsonInclude]
        public required float Radius;

        internal const int Size = Vec3.Size + sizeof(float);

        internal static RwSphere Read(BinaryReader s)
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
}
