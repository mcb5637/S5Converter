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
}
