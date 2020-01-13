using System;

namespace Fst {
    public abstract class BytesReader {
        public abstract long getPosition();

        public abstract void setPosition(long pos);

        public abstract Boolean reversed();

        public abstract byte readByte() ;

        public abstract void readBytes(out byte[] b, int offset, int len) ;

        public abstract void skipBytes(int num) ;
        
        public abstract int readInt() ;

    }
}