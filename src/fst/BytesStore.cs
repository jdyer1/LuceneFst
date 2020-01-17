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
            blocks.RemoveRange(blockIndex + 1, blocks.Count);
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
            if (allowSingle && blocks.Count == 1) {
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