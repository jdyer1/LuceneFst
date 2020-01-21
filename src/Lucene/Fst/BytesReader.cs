using System;
using System.Diagnostics;

namespace Lucene.Fst
{
    public abstract class BytesReader
    {

        private static readonly int SKIP_BUFFER_SIZE = 1024;
        private byte[] skipBuffer;
        public abstract long getPosition();

        public abstract void setPosition(long pos);

        public abstract bool reversed() ;

        public abstract byte readByte();

        public abstract void readBytes(byte[] b, int offset, int len);

        public abstract void skipBytes(int num);

        public void skipBytes(long numBytes)
        {
            if (numBytes < 0)
            {
                throw new ArgumentException("numBytes must be >= 0, got {0}", "" + numBytes);
            }
            if (this.skipBuffer == null)
            {
                this.skipBuffer = new byte[SKIP_BUFFER_SIZE];
            }
            Debug.Assert(skipBuffer.Length == SKIP_BUFFER_SIZE);
            for (long skipped = 0; skipped < numBytes;)
            {
                int step = (int)Math.Min(SKIP_BUFFER_SIZE, numBytes - skipped);
                readBytes(skipBuffer, 0, step);
                skipped += step;
            }
        }

        public int readInt()
        {
            return ((readByte() & 0xFF) << 24) | ((readByte() & 0xFF) << 16)
         | ((readByte() & 0xFF) << 8) | (readByte() & 0xFF);
        }

    }
}