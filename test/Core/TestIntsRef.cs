using System;
using Xunit;
using TestUtil;

namespace Lucene.Core
{

    public class TestIntsRef
    {

        [Fact]
        public void testEmpty()
        {
            IntsRef i = new IntsRef();
            Assert.Equal(IntsRef.EMPTY_INTS, i.ints);
            Assert.Equal(0, i.offset);
            Assert.Equal(0, i.length);
        }

        [Fact]
        public void testFromInts()
        {
            int[] ints = new int[] { 1, 2, 3, 4 };
            IntsRef i = new IntsRef(ints, 0, 4);
            Assert.Equal(ints, i.ints);
            Assert.Equal(0, i.offset);
            Assert.Equal(4, i.length);

            IntsRef i2 = new IntsRef(ints, 1, 3);
            Assert.Equal(new IntsRef(new int[] { 2, 3, 4 }, 0, 3), i2);

            Assert.False(i.Equals(i2));
        }


    }
}