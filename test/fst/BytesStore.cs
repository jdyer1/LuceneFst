using System;

namespace Fst {
    class BytesStore {

        readonly int blockbits;
        public BytesStore(int blockBits) {
            this.blockbits = blockbits;
        }

        public void writeByte(byte b) {
            //TOOD
        }

        public int getPosition() {
            return 0; //TODO
        }

        public void truncate(int len) {
            //TODO
        }

        public BytesReader getReverseReader() {
            return null; //TODO
        }

        public BytesReader getForwardReader() {
            return null; //TODO
        }
       
    }
}