using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class DebugWriteCheckStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                if (buffer[offset + i] != BytesToWrite[Offset])
                    throw new IOException($"write missmatch {buffer[offset + i]} != {BytesToWrite[Offset]}");
                ++Offset;
            }
        }

        internal required byte[] BytesToWrite;
        internal int Offset = 0;

        internal void CheckEnd()
        {
            if (BytesToWrite.Length != Offset)
                throw new IOException($"length missmatch {BytesToWrite.Length} != {Offset}");
        }
    }
}
