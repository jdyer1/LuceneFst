using System;
using System.Diagnostics;

namespace Lucene.Fst
{
    public class FST<T>
    {
        private readonly INPUT_TYPE inputType;
        public readonly Outputs<T> outputs;
        private readonly int bytesPageBits;
        private readonly BytesStore bytes;
        private T emptyOutput;
        public FST(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.outputs = outputs;
            this.bytesPageBits = bytesPageBits;
            this.bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            bytes.writeByte((byte)0);
        }

        public void setEmptyOutput(T v)
        {
            if (emptyOutput != null)
            {
                emptyOutput = outputs.merge(emptyOutput, v);
            }
            else
            {
                emptyOutput = v;
            }
        }
    }

    public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }
}