using System.Collections.Generic;
using System;
using Xunit;
using Serilog;
using TestUtil;

namespace Lucene.Fst
{
    public class FSTTester<T>
    {
        public static readonly ILogger log = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
        private readonly RandomTester r;
        private readonly int inputMode;
        private readonly List<InputOutput<T>> pairs;
        private readonly Outputs<T> outputs;
        private readonly bool doReverseLookup;

        public FSTTester(RandomTester r, int inputMode, List<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
        {
            this.r = r;
            this.inputMode = inputMode;
            this.pairs = pairs;
            this.outputs = outputs;
            this.doReverseLookup = doReverseLookup;
        }

        public void doTest()
        {
            INPUT_TYPE inputType = inputMode == 0 ? INPUT_TYPE.BYTE1 : INPUT_TYPE.BYTE4;
            Builder<T> builder = new Builder<T>()


        }
    }

}
}