using System.Text.Json.Serialization;

namespace S5Converter
{
    internal struct Vec2
    {
        [JsonPropertyName("x")]
        public required float X;
        [JsonPropertyName("y")]
        public required float Y;

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
}
