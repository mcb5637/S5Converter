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
        internal UInt32 Length;
        internal UInt32 Version;
        internal UInt32 BuildNum;

        internal const UInt32 rwLIBRARYCURRENTVERSION = 0x37002;
        internal const UInt32 rwLIBRARYBASEVERSION = 0x35000;

        internal static ChunkHeader Read(BinaryReader s)
        {
            ChunkHeader h = new()
            {
                Type = (RwCorePluginID)s.ReadUInt32(),
                Length = s.ReadUInt32(),
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
                s.ReadBytes((int)h.Length);
            }
        }
    }

    internal static class Helper
    {
        internal static string? ReadRWString(this BinaryReader s)
        {
            int l = s.ReadInt32();
            if (l == 0)
                return null;
            char[] c = s.ReadChars(l);
            ReadOnlySpan<char> r = new(c);
            if (r.Length > 1 && r[^1] == '\0')
                r = r[..^1];
            return new string(r);
        }

        internal static void WriteRWString(this BinaryWriter s, string v)
        {
            bool needs0 = v.EndsWith('\0');
            s.Write(v.Length + (needs0 ? 1 : 0));
            s.Write(v.AsSpan());
            if (needs0)
                s.Write('\0');
        }
    }
}
