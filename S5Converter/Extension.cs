using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace S5Converter
{
    internal abstract class Extension<T>
    {
        internal abstract int Size(T obj);
        internal abstract bool TryRead(BinaryReader s, ref ChunkHeader h, T obj);
        internal abstract void WriteExt(BinaryWriter s, T obj, UInt32 versionNum, UInt32 buildNum);

        internal int SizeH(T obj)
        {
            return ChunkHeader.Size + Size(obj);
        }
        internal void Read(BinaryReader s, T obj)
        {
            ChunkHeader exheader = ChunkHeader.FindChunk(s, RwCorePluginID.EXTENSION);
            while (exheader.Length > 0)
            {
                ChunkHeader h = ChunkHeader.Read(s);
                if (!TryRead(s, ref h, obj))
                {
                    Console.Error.WriteLine($"unknown extension {h.Type} on {typeof(T).Name}, skipping");
                    s.ReadBytes(h.Length);
                }
                exheader.Length -= h.Length + 12;
            }
        }
        internal void Write(BinaryWriter s, T obj, UInt32 versionNum, UInt32 buildNum)
        {
            new ChunkHeader()
            {
                Length = Size(obj),
                Type = RwCorePluginID.EXTENSION,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            WriteExt(s, obj, versionNum, buildNum);
        }

        // extensions:
        // camera & light: userdata
    }
}
