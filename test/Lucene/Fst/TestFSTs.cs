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
            }
            Array.Sort(terms);
            Array.Sort(terms2);
            log.Information("test");
        }

        private IntsRef toIntsRef(String s, int inputMode)
        {
            if (inputMode == 0)
            {
                BytesRef br = new BytesRef(s);
                int[] iArr = new int[br.length];
                for(int i=0 ; i<br.length ; i++) {
                    iArr[i] = (int) br.bytes[br.offset + i];
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