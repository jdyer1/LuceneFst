using System;
using System.Diagnostics;

namespace Lucene.Fst
{
    sealed class NoOutputs : Outputs<Object>
    {
        private static readonly NoOutput NO_OUTPUT = new NoOutput();

        private static readonly NoOutputs singleton = new NoOutputs();

        private NoOutputs() { }

        public static NoOutputs getSingleton()
        {
            return singleton;
        }


        public override Object common(Object output1, Object output2)
        {
            Debug.Assert(output1 == NO_OUTPUT);
            Debug.Assert(output2 == NO_OUTPUT);
            return NO_OUTPUT;
        }


        public override Object subtract(Object output, Object inc)
        {
            Debug.Assert(output == NO_OUTPUT);
            Debug.Assert(inc == NO_OUTPUT);
            return NO_OUTPUT;
        }


        public override Object add(Object prefix, Object output)
        {
            Debug.Assert(prefix == NO_OUTPUT, "got " + prefix);
            Debug.Assert(output == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public new Object merge(Object first, Object second)
        {
            Debug.Assert(first == NO_OUTPUT);
            Debug.Assert(second == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public override Object getNoOutput()
        {
            return NO_OUTPUT;
        }

        public override String outputToString(Object output)
        {
            return "";
        }

        public override String ToString()
        {
            return "NoOutputs";
        }
    }
}

sealed class NoOutput : IEquatable<Object>
{

    public override int GetHashCode()
    {
        return 42;
    }

    public override bool Equals(Object other)
    {
        return other == this;
    }

}