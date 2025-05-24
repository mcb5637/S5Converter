using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using S5Converter.Atomic;
using S5Converter.Frame;
using S5Converter.Geometry;

namespace S5Converter
{
    internal static class Helper
    {
        internal static string? ReadRWString(this BinaryReader s)
        {
            int l = s.ReadInt32();
            if (l == 0)
                return null;
            byte[] c = s.ReadBytes(l);
            ReadOnlySpan<byte> r = new(c);
            if (r.Length >= 1 && r[^1] == '\0')
                r = r[..^1];
            return Encoding.ASCII.GetString(r);
        }

        internal static void WriteRWString(this BinaryWriter s, string? v)
        {
            if (v == null)
            {
                s.Write(0);
                return;
            }
            if (v.Contains('\0'))
                throw new IOException("string contains \\0");
            byte[] d = Encoding.ASCII.GetBytes(v);
            s.Write(d.Length + 1);
            s.Write(d);
            s.Write((byte)0);
        }

        internal static int GetRWLength(this string? s)
        {
            if (s == null)
                return sizeof(int);
            return Encoding.ASCII.GetByteCount(s) + 1 + sizeof(int);
        }

        internal static void ReadArray<T>(this T[] a, Func<T> reader)
        {
            for (int i = 0; i < a.Length; ++i)
                a[i] = reader();
        }
        internal static void ReadArray<T>(this T[] a, BinaryReader s, Func<BinaryReader, T> reader)
        {
            for (int i = 0; i < a.Length; ++i)
                a[i] = reader(s);
        }
        internal static void WriteAsByte(this BinaryWriter s, int d)
        {
            if (d > byte.MaxValue || d < byte.MinValue)
                throw new IOException($"{d} too big for a byte");
            s.Write((byte)d);
        }

        internal static float RadToDegOpt(this float v, bool a)
        {
            if (a)
                return float.RadiansToDegrees(v);
            else
                return v;
        }
        internal static float DegToRadOpt(this float v, bool a)
        {
            if (a)
                return float.DegreesToRadians(v);
            else
                return v;
        }

        internal static string ReadFixedSizeString(this BinaryReader s, int size)
        {
            byte[] c = s.ReadBytes(size);
            ReadOnlySpan<byte> r = new(c);
            int i = Array.IndexOf(c, (byte)0);
            if (r.Length > 1 && i < r.Length)
                r = r[..i];
            return Encoding.ASCII.GetString(r);
        }
        internal static void WriteFixedSizeString(this BinaryWriter s, string str, int size)
        {
            byte[] b = Encoding.ASCII.GetBytes(str);
            if (b.Length > size - 1) // leading 0 is probably needed
                throw new IOException($"fixed size string {str} too long {size}");
            s.Write(b);
            for (int i = b.Length; i < size; ++i)
                s.Write((byte)0);
        }
        // binary ops on enums dont work on generics
        internal static void SetFlag(this ref RwMatrix.MatrixFlagsS.MatrixFlags e, bool v, RwMatrix.MatrixFlagsS.MatrixFlags f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
        internal static void SetFlag(this ref RpAtomic.AtomicFlagsS.AtomicFlags e, bool v, RpAtomic.AtomicFlagsS.AtomicFlags f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
        internal static void SetFlag(this ref RpGeometry.GeometryFlagS.RpGeometryFlag e, bool v, RpGeometry.GeometryFlagS.RpGeometryFlag f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
        internal static void SetFlag(this ref RpHAnimHierarchy.RpHAnimHierarchyFlagS.RpHAnimHierarchyFlag e, bool v, RpHAnimHierarchy.RpHAnimHierarchyFlagS.RpHAnimHierarchyFlag f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
        internal static void SetFlag(this ref RpMeshHeader.MeshHeaderFlags.RpMeshHeaderFlags e, bool v, RpMeshHeader.MeshHeaderFlags.RpMeshHeaderFlags f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
    }
    internal class EnumJsonConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? s = reader.GetString();
            if (Enum.TryParse<T>(s, out var r))
                return r;
            throw new JsonException($"invalid enum value {s}");
        }
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
