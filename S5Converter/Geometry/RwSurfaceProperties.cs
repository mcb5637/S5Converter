using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal struct RwSurfaceProperties
    {
        [JsonPropertyName("ambient")]
        public float Ambient;
        [JsonPropertyName("specular")]
        public float Specular;
        [JsonPropertyName("diffuse")]
        public float Diffuse;

        internal const int Size = sizeof(float) * 3;
    }
}
