using System.Text.Json.Serialization;

namespace S5Converter
{
    internal class RwMatrix
    {
        public struct MatrixFlagsS
        {
            [Flags]
            internal enum MatrixFlags : int
            {
                None = 0,
                rwMATRIXTYPENORMAL = 0x00000001,
                rwMATRIXTYPEORTHOGONAL = 0x00000002,
                rwMATRIXTYPEORTHONORMAL = 0x00000003,
                rwMATRIXINTERNALIDENTITY = 0x00020000,
            }
            internal MatrixFlags Flags;

            [JsonRequired]
            public bool Normal
            {
                readonly get => Flags.HasFlag(MatrixFlags.rwMATRIXTYPENORMAL);
                set => Flags.SetFlag(value, MatrixFlags.rwMATRIXTYPENORMAL);
            }
            [JsonRequired]
            public bool Orthogonal
            {
                readonly get => Flags.HasFlag(MatrixFlags.rwMATRIXTYPEORTHOGONAL);
                set => Flags.SetFlag(value, MatrixFlags.rwMATRIXTYPEORTHOGONAL);
            }
            [JsonRequired]
            public bool Identity
            {
                readonly get => Flags.HasFlag(MatrixFlags.rwMATRIXINTERNALIDENTITY);
                set => Flags.SetFlag(value, MatrixFlags.rwMATRIXINTERNALIDENTITY);
            }
        }
        public required Vec3 Right;
        public required Vec3 Up;
        public required Vec3 At;
        public required Vec3 Pos;
        public required MatrixFlagsS Flags;

        private const int SizeData = Vec3.Size * 4 + sizeof(int);
        internal const int Size = SizeData + ChunkHeader.Size;
        internal const int SizeH = Size + ChunkHeader.Size;

        internal static RwMatrix Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.MATRIX);
            if (ChunkHeader.FindChunk(s, RwCorePluginID.STRUCT).Length != SizeData)
                throw new IOException("matrix invalid struct size");
            return new()
            {
                Right = Vec3.Read(s),
                Up = Vec3.Read(s),
                At = Vec3.Read(s),
                Pos = Vec3.Read(s),
                Flags = new()
                {
                    Flags = (MatrixFlagsS.MatrixFlags)s.ReadInt32(),
                },
            };
        }

        internal void Write(BinaryWriter s, bool header, UInt32 versionNum, UInt32 buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.MATRIX,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            new ChunkHeader()
            {
                Length = SizeData,
                Type = RwCorePluginID.STRUCT,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
            Right.Write(s);
            Up.Write(s);
            At.Write(s);
            Pos.Write(s);
            s.Write((int)Flags.Flags);
        }
    }

    internal class RwMatrixRaw : RwMatrix
    {
        public int Pad1 = 0;
        public int Pad2 = 0;
        public int Pad3 = 0;

        internal new const int Size = Vec3.Size * 4 + sizeof(int) * 4;
        internal new const int SizeH = Size;

        internal static RwMatrixRaw Read(BinaryReader s)
        {
            return new()
            {
                Right = Vec3.Read(s),
                Flags = new()
                {
                    Flags = (MatrixFlagsS.MatrixFlags)s.ReadInt32(),
                },
                Up = Vec3.Read(s),
                Pad1 = s.ReadInt32(),
                At = Vec3.Read(s),
                Pad2 = s.ReadInt32(),
                Pos = Vec3.Read(s),
                Pad3 = s.ReadInt32()
            };
        }
        internal void Write(BinaryWriter s)
        {
            Right.Write(s);
            s.Write((int)Flags.Flags);
            Up.Write(s);
            s.Write(Pad1);
            At.Write(s);
            s.Write(Pad2);
            Pos.Write(s);
            s.Write(Pad3);
        }
    }
}
