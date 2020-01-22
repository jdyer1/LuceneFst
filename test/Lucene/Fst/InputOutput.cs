using System;
using Lucene.Core;

namespace Lucene.Fst
{
    public class InputOutput<T> : IComparable<InputOutput<T>> {
        public readonly IntsRef input;
        public readonly T output;

        public InputOutput(IntsRef input, T output) {
            this.input = input;
            this.output = output;
        }

        public int CompareTo(InputOutput<T> that)
        {
            return this.input.CompareTo(that.input);
        }
    }
}