using System;

namespace Fst
{
    public class ReverseBytesReader : BytesReader
    {
        private readonly byte[] bytes;
        private int pos;
        public ReverseBytesReader(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override byte readByte()
        {
            return bytes[pos--];
        }

        public override void readBytes(byte[] b, int offset, int len)
        {
            for (int i = 0; i < len; i++)
            {
                b[offset + i] = bytes[pos--];
            }
        }

        public override void skipBytes(int count)
        {
            pos -= count;
        }

        public override long getPosition()
        {
            return (long)pos;
        }

        public override void setPosition(long pos)
        {
            this.pos = (int)pos;
        }

        public override bool reversed() => true;
    }
}