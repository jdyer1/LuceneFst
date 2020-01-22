using System;

namespace Lucene.Fst
{
    public abstract class Outputs<T>
    {
        public abstract T common(T output1, T output2);

        public abstract T subtract(T output, T inc);

        public abstract T add(T prefix, T output);

        public abstract T getNoOutput();

        public abstract String outputToString(T output);

        public T merge(T first, T second)
        {
            throw new NotSupportedException();
        }

        //TODO: input/output methods
        //TODO: ramBytesUsed
    }
}
