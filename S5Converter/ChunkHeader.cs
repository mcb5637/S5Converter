using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace S5Converter
{
    internal struct ChunkHeader
    {
        internal required RwCorePluginID Type;
        internal required int Length;
        internal required UInt32 Version;
        internal required UInt32 BuildNum;

        internal const UInt32 rwLIBRARYCURRENTVERSION = 0x37002;
        internal const UInt32 rwLIBRARYBASEVERSION = 0x35000;
        internal const UInt32 DefaultBuildNum = 10;

        internal const int Size = 3 * sizeof(int);

        public ChunkHeader()
        {
        }

        internal static ChunkHeader Read(BinaryReader s)
        {
            ChunkHeader h = new()
            {
                Type = (RwCorePluginID)s.ReadUInt32(),
                Length = s.ReadInt32(),
                BuildNum = 0,
                Version = 0,
            };
            UInt32 lib = s.ReadUInt32();

            if ((lib & 0xffff0000) == 0) // old lib id
            {
                h.Version = lib << 8;
                h.BuildNum = 0;
            }
            else
            {
                h.Version = (((lib >> 14) & 0x3ff00U) + 0x30000U) | ((lib >> 16) & 0x0003fU);
                h.BuildNum = lib & 0xffffU;
            }
            return h;
        }

        internal readonly void Write(BinaryWriter s)
        {
            s.Write((UInt32)Type);
            s.Write(Length);
            s.Write((((Version - 0x30000U) & 0x3ff00U) << 14) | ((Version & 0x0003fU) << 16) | (BuildNum & 0xffffU));
        }

        internal static ChunkHeader FindChunk(BinaryReader s, RwCorePluginID type)
        {
            while (true)
            {
                ChunkHeader h = Read(s);
                if (h.Type == type)
                {
                    if (h.Version > rwLIBRARYCURRENTVERSION || h.Version < rwLIBRARYBASEVERSION)
                        throw new IOException($"{h.Version} bad version for header of type {type}");
                    return h;
                }
                Console.Error.WriteLine($"warning: skipping chunk {h.Type}, searching for {type}");
                s.ReadBytes(h.Length);
            }
        }

        internal static (string, int[]) FindAndReadString(BinaryReader s)
        {
            while (true)
            {
                ChunkHeader h = Read(s);
                if (h.Version > rwLIBRARYCURRENTVERSION || h.Version < rwLIBRARYBASEVERSION)
                    throw new IOException($"{h.Version} bad version for string header");
                if (h.Type == RwCorePluginID.STRING)
                {
                    byte[] c = s.ReadBytes(h.Length);
                    ReadOnlySpan<byte> r = new(c);
                    int i = Array.IndexOf(c, (byte)0);
                    int[] p = r[i..].ToArray().Select(x => (int)x).ToArray();
                    if (r.Length > 1 && i < r.Length)
                        r = r[..i];
                    return (Encoding.ASCII.GetString(r), p);
                }
                else if (h.Type == RwCorePluginID.UNICODESTRING) // is there a difference?
                {
                    byte[] c = s.ReadBytes(h.Length);
                    ReadOnlySpan<byte> r = new(c);
                    int i = Array.IndexOf(c, (byte)0);
                    int[] p = r[i..].ToArray().Select(x => (int)x).ToArray();
                    if (r.Length > 1 && i < r.Length)
                        r = r[..i];
                    return (Encoding.UTF8.GetString(r), p);
                }
                Console.Error.WriteLine($"warning: skipping chunk {h.Type}, searching for string/unicode string");
                s.ReadBytes(h.Length);
            }
        }

        internal static void WriteString(BinaryWriter s, string str, int[]? padding, UInt32 versionNum, UInt32 buildNum)
        {
            if (str.Contains('\0'))
                throw new IOException("string contains \\0");
            ChunkHeader h;
            byte[] b;
            if (str.All(char.IsAscii))
            {
                b = Encoding.ASCII.GetBytes(str);
                h = new()
                {
                    Type = RwCorePluginID.STRING,
                    Length = b.Length + 1,
                    BuildNum = buildNum,
                    Version = versionNum,
                };
            }
            else
            {
                b = Encoding.UTF8.GetBytes(str);
                h = new()
                {
                    Type = RwCorePluginID.UNICODESTRING,
                    Length = b.Length + 1,
                    BuildNum = buildNum,
                    Version = versionNum,
                };
            }
            int extra0 = 0;
            while ((h.Length + extra0) % sizeof(int) != 0)
                ++extra0;
            h.Length += extra0;
            h.Write(s);
            s.Write(b);
            if (padding != null && padding.Length >= 1)
                padding[0] = 0;
            for (int i = 0; i < (extra0 + 1); ++i)
            {
                int p = 0;
                if (padding != null && i < padding.Length)
                    p = padding[i];
                s.Write((byte)p);
            }
        }

        internal static int GetStringSize(string str)
        {
            int l;
            if (str.All(char.IsAscii))
            {
                l = Encoding.ASCII.GetByteCount(str) + 1;
            }
            else
            {
                l = Encoding.UTF8.GetByteCount(str) + 1;
            }
            int extra0 = 0;
            while ((l + extra0) % sizeof(int) != 0)
                ++extra0;
            return l + extra0 + Size;
        }
    }

    internal interface IDictEntry<T>
    {
        static abstract RwCorePluginID DictId { get; }
        abstract int SizeH { get; }
        static abstract T Read(BinaryReader s, bool header);
        void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum);
    }

    internal static class RwDict
    {
        internal static int GetSize<T>(T[] data) where T : IDictEntry<T>
        {
            return data.Sum(x => x.SizeH) + ChunkHeader.Size + sizeof(int);
        }
        internal static T[] Read<T>(BinaryReader s, bool header) where T : IDictEntry<T>
        {
            if (header)
                ChunkHeader.FindChunk(s, T.DictId);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != sizeof(int))
                throw new IOException("dict struct length missmatch");
            int nelems = s.ReadInt32();
            T[] r = new T[nelems];
            r.ReadArray(() => T.Read(s, true));
            return r;
        }
        internal static void Write<T>(T[] data, BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum) where T : IDictEntry<T>
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Type = T.DictId,
                    Length = GetSize(data),
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Type = RwCorePluginID.STRUCT,
                Length = sizeof(int),
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            s.Write(data.Length);
            foreach (T e in data)
                e.Write(s, true, versionNum, buildNum);
        }
    }

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
        internal static void SetFlag(this ref Atomic.AtomicFlagsS.AtomicFlags e, bool v, Atomic.AtomicFlagsS.AtomicFlags f)
        {
            if (v)
                e |= f;
            else
                e &= ~f;
        }
        internal static void SetFlag(this ref Geometry.GeometryFlagS.RpGeometryFlag e, bool v, Geometry.GeometryFlagS.RpGeometryFlag f)
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
