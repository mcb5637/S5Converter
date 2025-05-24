using System.Text.Json.Serialization;

namespace S5Converter
{
    internal struct RGBAF
    {
        [JsonPropertyName("red")]
        public float Red;
        [JsonPropertyName("green")]
        public float Green;
        [JsonPropertyName("blue")]
        public float Blue;
        [JsonPropertyName("alpha")]
        public float Alpha;

        internal const int Size = 4 * sizeof(float);

        internal static RGBAF Read(BinaryReader s)
        {
            return new()
            {
                Red = s.ReadSingle(),
                Green = s.ReadSingle(),
                Blue = s.ReadSingle(),
                Alpha = s.ReadSingle(),
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
