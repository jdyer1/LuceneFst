using System.Collections.Generic;
using System;
using Xunit;
using TestUtil;
using Serilog;

using Lucene.Core;

namespace Lucene.Fst
{
    public class TestFSTs
    {
        public static readonly ILogger log = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
        private const int seed = 1;
        private readonly RandomTester r = new RandomTester(seed);

        [Fact]
        public void testBasicFSA()
        {
            String[] strings = new String[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat" };
            String[] strings2 = new String[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation" };
            IntsRef[] terms = new IntsRef[strings.Length];
            IntsRef[] terms2 = new IntsRef[strings2.Length];
            for (int inputMode = 0; inputMode < 1; inputMode++) //TODO: inputMode=2
            {
                log.Debug("> inputMode={inputMode}", inputMode);

                for (int idx = 0; idx < strings.Length; idx++)
                {
                    terms[idx] = toIntsRef(strings[idx], inputMode);
                }
                for (int idx = 0; idx < strings2.Length; idx++)
                {
                    terms2[idx] = toIntsRef(strings2[idx], inputMode);
                }

                Array.Sort(terms);
                Array.Sort(terms2);

                Outputs<Object> outputs = NoOutputs.getSingleton();
                Object NO_OUTPUT = outputs.getNoOutput();
                List<InputOutput<Object>> pairs = new List<InputOutput<object>>();
                foreach (IntsRef term in terms)
                {
                    pairs.Add(new InputOutput<object>(term, NO_OUTPUT));
                }
                new FSTTester<Object>(r, inputMode, pairs, outputs, false).doTest();
            }

        }

        private IntsRef toIntsRef(String s, int inputMode)
        {
            if (inputMode == 0)
            {
                BytesRef br = new BytesRef(s);
                int[] iArr = new int[br.length];
                for (int i = 0; i < br.length; i++)
                {
                    iArr[i] = (int)br.bytes[br.offset + i];
                }
                IntsRef ir = new IntsRef(iArr, 0, br.length);
                return ir;
            }
            else
            {
                throw new NotImplementedException("TODO inputmode 1, utf-32");
            }
        }


    }

}