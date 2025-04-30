using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5Converter
{
    internal struct ChunkHeader
    {
        internal RwCorePluginID Type;
        internal int Length;
        internal UInt32 Version = rwLIBRARYCURRENTVERSION;
        internal UInt32 BuildNum = 10;

        internal const UInt32 rwLIBRARYCURRENTVERSION = 0x37002;
        internal const UInt32 rwLIBRARYBASEVERSION = 0x35000;

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
                s.ReadBytes((int)h.Length);
            }
        }

        internal static string FindAndReadString(BinaryReader s)
        {
            while (true)
            {
                ChunkHeader h = Read(s);
                if (h.Version > rwLIBRARYCURRENTVERSION || h.Version < rwLIBRARYBASEVERSION)
                    throw new IOException($"{h.Version} bad version for string header");
                if (h.Type == RwCorePluginID.STRING)
                {
                    byte[] c = s.ReadBytes((int)h.Length);
                    ReadOnlySpan<byte> r = new(c);
                    int i = Array.IndexOf(c, (byte)0);
                    if (r.Length > 1 && i < r.Length)
                        r = r[..i];
                    return Encoding.ASCII.GetString(r);
                }
                else if (h.Type == RwCorePluginID.UNICODESTRING) // is there a difference?
                {
                    byte[] c = s.ReadBytes((int)h.Length);
                    ReadOnlySpan<byte> r = new(c);
                    int i = Array.IndexOf(c, (byte)0);
                    if (r.Length > 1 && i < r.Length)
                        r = r[..i];
                    return Encoding.UTF8.GetString(r);
                }
                Console.Error.WriteLine($"warning: skipping chunk {h.Type}, searching for string/unicode string");
                s.ReadBytes((int)h.Length);
            }
        }

        internal static void WriteString(BinaryWriter s, string str)
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
                };
            }
            else
            {
                b = Encoding.UTF8.GetBytes(str);
                h = new()
                {
                    Type = RwCorePluginID.UNICODESTRING,
                    Length = b.Length + 1,
                };
            }
            int extra0 = 0;
            while ((h.Length + extra0) % sizeof(int) != 0)
                ++extra0;
            h.Length += extra0;
            h.Write(s);
            s.Write(b);
            for (int i = 0; i < (extra0 + 1); ++i)
                s.Write((byte)0);
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

    internal static class Helper
    {
        internal static string? ReadRWString(this BinaryReader s)
        {
            int l = s.ReadInt32();
            if (l == 0)
                return null;
            byte[] c = s.ReadBytes(l);
            ReadOnlySpan<byte> r = new(c);
            if (r.Length > 1 && r[^1] == '\0')
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

        internal static bool IsFlagSet(this Geometry.RpGeometryFlag f, Geometry.RpGeometryFlag check)
        {
            return (f & check) != 0;
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
    }
}
