using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal struct RpTriangle
    {
        [JsonPropertyName("v1")]
        public short V1;
        [JsonPropertyName("v2")]
        public short V2;
        [JsonPropertyName("v3")]
        public short V3;
        [JsonPropertyName("materialId")]
        public short MaterialId;

        internal const int Size = sizeof(short) * 4;

        internal static RpTriangle Read(BinaryReader s)
        {
            return new() // original code reads as is and then swaps around
            {
                V2 = s.ReadInt16(),
                V1 = s.ReadInt16(),
                MaterialId = s.ReadInt16(),
                V3 = s.ReadInt16(),
            };
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write(V2);
            s.Write(V1);
            s.Write(MaterialId);
            s.Write(V3);
        }
    }
}
