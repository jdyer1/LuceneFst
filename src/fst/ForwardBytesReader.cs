using System;

namespace Fst {
    public class ForwardBytesReader : BytesReader
    {
        private readonly byte[] bytes;
        private int pos;
        public ForwardBytesReader(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override byte readByte() {
            return bytes[pos++];
        }

        public override void readBytes(byte[] b, int offset, int len) {
            Array.Copy(this.bytes, pos, b, offset, len);
            pos += len;
        }

        public override void skipBytes(int count) {
            pos += count;
        }

        public override long getPosition() {
            return (long) pos;
        }

        public override void setPosition(long pos) {
            this.pos = (int) pos;
        }

        public override bool reversed() => false;
    }
}