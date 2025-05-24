using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal struct RwTexCoords
    {
        [JsonPropertyName("u")]
        public float U;
        [JsonPropertyName("v")]
        public float V;

        internal const int Size = 2 * sizeof(float);

        internal static RwTexCoords Read(BinaryReader s)
        {
            return new()
            {
                U = s.ReadSingle(),
                V = s.ReadSingle(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(U);
            s.Write(V);
        }
    }
}
