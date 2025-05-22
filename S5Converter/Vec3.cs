using System.Text.Json.Serialization;

namespace S5Converter
{
    internal struct Vec3
    {
        [JsonPropertyName("x")]
        public required float X;
        [JsonPropertyName("y")]
        public required float Y;
        [JsonPropertyName("z")]
        public required float Z;

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
}
