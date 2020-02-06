using System;
using Lucene.Core;

namespace Lucene.Fst
{
    /// Reusable buffer for building nodes with fixed length arcs 
    /// (binary search or direct addressing).
    public class FixedLengthArcsBuffer
    {
        /// Initial capacity is the max length required for the header 
        /// of a node with fixed length arcs:
        /// header(byte) + numArcs(vint) + numBytes(vint)
        public byte[] bytes;
        public readonly ByteArrayDataOutput bado;

        public FixedLengthArcsBuffer()
        {
            this.bytes = new byte[11];
            this.bado = new ByteArrayDataOutput(bytes);
        }

        public FixedLengthArcsBuffer resetPosition()
        {
            bado.reset(bytes);
            return this;
        }
        public int getPosition()
        {
            return bado.getPosition();
        }

        public FixedLengthArcsBuffer writeByte(byte b)
        {
            bado.writeByte(b);
            return this;
        }

        public FixedLengthArcsBuffer writeVInt(int i)
        {
            bado.writeVInt(i);
            return this;
        }

        public byte[] getBytes()
        {
            return bytes;
        }

    }
}