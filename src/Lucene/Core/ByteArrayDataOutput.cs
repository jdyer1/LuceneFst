using System;
using System.Diagnostics;

namespace Lucene.Core
{
    public class ByteArrayDataOutput : DataOutput
    {
        private byte[] bytes;
        private int pos;
        private int limit;

        public ByteArrayDataOutput(byte[] bytes)
        {
            reset(bytes);
        }

        public void reset(byte[] bytes)
        {
            reset(bytes, 0, bytes.Length);
        }
        public void reset(byte[] bytes, int offset, int len)
        {
            this.bytes = bytes;
            pos = offset;
            limit = offset + len;
        }

        public int getPosition()
        {
            return pos;
        }

        public override void writeByte(byte b)
        {
            Debug.Assert(pos < limit);
            bytes[pos++] = b;
        }

        public override void writeBytes(byte[] b, int offset, int length)
        {
            Debug.Assert(pos + length <= limit);
            Array.Copy(b, offset, bytes, pos, length);
            pos += length;
        }

    }
}