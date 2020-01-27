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
        private long nodeCount;
        private long arcCount;

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
            Builder<T> builder = new Builder<T>(inputType, outputs, 15);
            foreach (InputOutput<T> pair in pairs)
            {
                builder.add(pair.input, pair.output);
            }
            FST<T> fst = builder.finish();
            if (fst == null)
            {
                log.Information("> fst has 0 nodes (fully pruned)");
            }
            else
            {
                log.Information("> fst has {nodecount} nodes and {arccount} arcs.", builder.getNodeCount(), builder.getArcCount());
            }

            //TODO: verifyUnPruned(inputMode, fst);

            nodeCount = builder.getNodeCount();
            arcCount = builder.getArcCount();

        }
    }

}
}