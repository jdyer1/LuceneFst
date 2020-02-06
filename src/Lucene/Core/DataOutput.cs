using System;

namespace Lucene.Core
{
    public abstract class DataOutput
    {
        public abstract void writeByte(byte b);

        public void writeBytes(byte[] b, int length)
        {
            writeBytes(b, 0, length);
        }
        public abstract void writeBytes(byte[] b, int offset, int length);


        public void writeInt(int i)
        {
            writeByte((byte)(i >> 24));
            writeByte((byte)(i >> 16));
            writeByte((byte)(i >> 8));
            writeByte((byte)i);
        }

        public void writeShort(short i)
        {
            writeByte((byte)(i >> 8));
            writeByte((byte)i);
        }
        
        /// See https://lucene.apache.org/core/8_4_1/core/org/apache/lucene/store/DataOutput.html#writeVInt-int-
        /// for thorough documentation.
        public void writeVInt(int i)
        {
            while ((i & ~0x7F) != 0)
            {
                writeByte((byte)((i & 0x7F) | 0x80));
                i = ((ushort)i) >> 7;
            }
            writeByte((byte)i);
        }

        public void writeVLong(long i)
        {
            if (i < 0)
            {
                throw new ArgumentException("cannot write negative vLong (got: " + i + ")");
            }
            writeSignedVLong(i);
        }

        // write a potentially negative vLong
        private void writeSignedVLong(long i)
        {
            while ((i & ~0x7FL) != 0L)
            {
                writeByte((byte)((i & 0x7FL) | 0x80L));
                i = ((uint)i) >> 7;
            }
            writeByte((byte)i);
        }

    }
}