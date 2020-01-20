using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace Fst
{
    class BytesStore
    {


        private readonly List<byte[]> blocks = new List<byte[]>();
        private readonly int blockSize;
        private readonly int blockBits;
        private readonly int blockMask;

        private byte[] current;
        private int nextWrite;


        public BytesStore(int blockBits)
        {
            this.blockBits = blockBits;
            blockSize = 1 << blockBits;
            blockMask = blockSize - 1;
            nextWrite = blockSize;
        }

        public void writeByte(long dest, byte b)
        {
            int blockIndex = (int)(dest >> blockBits);
            byte[] block = blocks[blockIndex];
            block[(int)(dest & blockMask)] = b;
        }

        public void writeByte(byte b)
        {
            if (nextWrite == blockSize)
            {
                current = new byte[blockSize];
                blocks.Add(current);
                nextWrite = 0;
            }
            current[nextWrite++] = b;
        }

        public void writeBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    Debug.Assert(b != null);
                    Debug.Assert(current != null);
                    Array.Copy(b, offset, current, nextWrite, len);
                    nextWrite += len;
                    break;
                }
                else
                {
                    if (chunk > 0)
                    {
                        Array.Copy(b, offset, current, nextWrite, chunk);
                        offset += chunk;
                        len -= chunk;
                    }
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }

        public void writeBytes(long dest, byte[] b, int offset, int len)
        {
            Debug.Assert(dest+len <= getPosition());
            
            long end = dest + len;
            int blockIndex = (int)(end >> blockBits);
            int downTo = (int)(end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;
                downTo = blockSize;
            }
            byte[] block = blocks[blockIndex];

            while (len > 0)
            {
                if (len <= downTo)
                {
                    Array.Copy(b, offset, block, downTo - len, len);
                    break;
                }
                else
                {
                    len -= downTo;
                    Array.Copy(b, offset + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        public void copyBytes(long src, long dest, int len)
        {
            Debug.Assert(src < dest);

            long end = src + len;

            int blockIndex = (int)(end >> blockBits);
            int downTo = (int)(end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;
                downTo = blockSize;
            }
            byte[] block = blocks[blockIndex];

            while (len > 0)
            {
                if (len <= downTo)
                {
                    writeBytes(dest, block, downTo - len, len);
                    break;
                }
                else
                {
                    len -= downTo;
                    writeBytes(dest + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        public void skipBytes(int len)
        {
            while (len > 0)
            {
                int chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    nextWrite += len;
                    break;
                }
                else
                {
                    len -= chunk;
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }
        public void writeInt(long pos, int value)
        {
            int blockIndex = (int)(pos >> blockBits);
            int upTo = (int)(pos & blockMask);
            byte[] block = blocks[blockIndex];
            int shift = 24;
            for (int i = 0; i < 4; i++)
            {
                block[upTo++] = (byte)(value >> shift);
                shift -= 8;
                if (upTo == blockSize)
                {
                    upTo = 0;
                    blockIndex++;
                    block = blocks[blockIndex];
                }
            }
        }

        public void reverse(long srcPos, long destPos)
        {
            Debug.Assert(srcPos < destPos);
            Debug.Assert(destPos < getPosition());

            int srcBlockIndex = (int)(srcPos >> blockBits);
            int src = (int)(srcPos & blockMask);
            byte[] srcBlock = blocks[srcBlockIndex];

            int destBlockIndex = (int)(destPos >> blockBits);
            int dest = (int)(destPos & blockMask);
            byte[] destBlock = blocks[destBlockIndex];

            int limit = (int)(destPos - srcPos + 1) / 2;
            for (int i = 0; i < limit; i++)
            {
                byte b = srcBlock[src];
                srcBlock[src] = destBlock[dest];
                destBlock[dest] = b;
                src++;
                if (src == blockSize)
                {
                    srcBlockIndex++;
                    srcBlock = blocks[srcBlockIndex];
                    src = 0;
                }

                dest--;
                if (dest == -1)
                {
                    destBlockIndex--;
                    destBlock = blocks[destBlockIndex];
                    dest = blockSize - 1;
                }
            }
        }


        public long getPosition()
        {
            return ((long)blocks.Count - 1) * blockSize + nextWrite;
        }

        public void truncate(long newLen)
        {
            Debug.Assert(newLen <= getPosition());
            Debug.Assert(newLen >= 0);
            int blockIndex = (int)(newLen >> blockBits);
            nextWrite = (int)(newLen & blockMask);
            if (nextWrite == 0)
            {
                blockIndex--;
                nextWrite = blockSize;
            }
            int from = blockIndex + 1;
            if(from < blocks.Count) {
                int count = blocks.Count - from;            
                blocks.RemoveRange(from, count);
            }
            if (newLen == 0)
            {
                current = null;
            }
            else
            {
                current = blocks[blockIndex];
            }
            Debug.Assert(newLen == getPosition());
        }

        public BytesReader getReverseReader()
        {
            return getReverseReader(true);
        }

        BytesReader getReverseReader(Boolean allowSingle)
        {
            if (allowSingle && blocks.Count == 1)
            {
                return new ReverseBytesReader(blocks[0]);
            }
            return new BytesStoreReverseReader(this);
        }

        public BytesReader getForwardReader()
        {
            if (blocks.Count == 1)
            {
                return new ForwardBytesReader(blocks[0]);
            }
            return new BytesStoreForwardReader(this);
        }

        class BytesStoreForwardReader : BytesReader
        {

            private BytesStore bytesStore;
            private byte[] current;
            private int nextBuffer;
            private int nextRead;

            public BytesStoreForwardReader(BytesStore bytesStore)
            {
                this.bytesStore = bytesStore;
                this.nextRead = bytesStore.blockSize;
            }

            public override byte readByte()
            {
                if (nextRead == bytesStore.blockSize)
                {
                    current = bytesStore.blocks[nextBuffer++];
                    nextRead = 0;
                }
                return current[nextRead++];
            }

            public override void skipBytes(int count)
            {
                setPosition(getPosition() + count);
            }

            public override void readBytes(byte[] b, int offset, int len)
            {
                while (len > 0)
                {
                    int chunkLeft = bytesStore.blockSize - nextRead;
                    if (len <= chunkLeft)
                    {
                        Array.Copy(current, nextRead, b, offset, len);
                        nextRead += len;
                        break;
                    }
                    else
                    {
                        if (chunkLeft > 0)
                        {
                            Array.Copy(current, nextRead, b, offset, chunkLeft);
                            offset += chunkLeft;
                            len -= chunkLeft;
                        }
                        current = bytesStore.blocks[nextBuffer++];
                        nextRead = 0;
                    }
                }
            }
            public override long getPosition()
            {
                return ((long)nextBuffer - 1) * bytesStore.blockSize + nextRead;
            }

            public override void setPosition(long pos)
            {
                int bufferIndex = (int)(pos >> bytesStore.blockBits);
                nextBuffer = bufferIndex + 1;
                current = bytesStore.blocks[bufferIndex];
                nextRead = (int)(pos & bytesStore.blockMask);
                Debug.Assert(getPosition() == pos);
            }

            public override bool reversed()
            {
                return false;
            }
        }
        class BytesStoreReverseReader : BytesReader
        {

            private BytesStore bytesStore;
            private byte[] current;
            private int nextBuffer;
            private int nextRead;

            public BytesStoreReverseReader(BytesStore bytesStore)
            {
                this.bytesStore = bytesStore;
                this.nextRead = 0;
                this.nextBuffer = -1;
                this.current = bytesStore.blocks.Count == 0 ? null : bytesStore.blocks[0];
            }

            public override byte readByte()
            {
                if (nextRead == -1)
                {
                    current = bytesStore.blocks[nextBuffer--];
                    nextRead = bytesStore.blockSize - 1;
                }
                return current[nextRead--];
            }

            public override void skipBytes(int count)
            {
                setPosition(getPosition() - count);
            }

            public override void readBytes(byte[] b, int offset, int len)
            {
                for (int i = 0; i < len; i++)
                {
                    b[offset + i] = readByte();
                }
            }
            public override long getPosition()
            {
                return ((long)nextBuffer + 1) * bytesStore.blockSize + nextRead;
            }

            public override void setPosition(long pos)
            {
                int bufferIndex = (int)(pos >> bytesStore.blockBits);
                nextBuffer = bufferIndex - 1;
                current = bytesStore.blocks[bufferIndex];
                nextRead = (int)(pos & bytesStore.blockMask);
                Debug.Assert(getPosition() == pos);
            }

            public override bool reversed()
            {
                return true;
            }
        }

    }

}