using System.Text.Json.Serialization;

namespace S5Converter
{
    internal struct RGBA
    {
        [JsonPropertyName("red")]
        public byte Red;
        [JsonPropertyName("green")]
        public byte Green;
        [JsonPropertyName("blue")]
        public byte Blue;
        [JsonPropertyName("alpha")]
        public byte Alpha;

        internal const int Size = 4 * sizeof(byte);

        internal static RGBA Read(BinaryReader s)
        {
            return new()
            {
                Red = s.ReadByte(),
                Green = s.ReadByte(),
                Blue = s.ReadByte(),
                Alpha = s.ReadByte(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(Red);
            s.Write(Green);
            s.Write(Blue);
            s.Write(Alpha);
        }
    }
}
