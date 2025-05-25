using System.Text.Json.Serialization;

namespace S5Converter.Anim
{
    struct RtQuat
    {
        public required Vec3 Imaginary;
        public required float Real;

        internal const int Size = Vec3.Size + sizeof(float);

        internal static RtQuat Read(BinaryReader s)
        {
            return new()
            {
                Imaginary = Vec3.Read(s),
                Real = s.ReadSingle(),
            };
        }
        internal readonly void Write(BinaryWriter s)
        {
            Imaginary.Write(s);
            s.Write(Real);
        }
    }
}
