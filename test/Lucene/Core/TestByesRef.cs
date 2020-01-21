using System;
using Xunit;
using TestUtil;

namespace Lucene.Core
{

    public class TestBytesRef
    {

        [Fact]
        public void testEmpty()
        {
            BytesRef b = new BytesRef();
            Assert.Equal(BytesRef.EMPTY_BYTES, b.bytes);
            Assert.Equal(0, b.offset);
            Assert.Equal(0, b.length);
        }

        [Fact]
        public void testFromBytes()
        {
            byte[] bytes = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes);
            Assert.Equal(bytes, b.bytes);
            Assert.Equal(0, b.offset);
            Assert.Equal(4, b.length);

            BytesRef b2 = new BytesRef(bytes, 1, 3);
            Assert.Equal("bcd", b2.utf8ToString());

            Assert.False(b.Equals(b2));
        }
        [Fact]
        public void testCompare()
        {
            BytesRef br1 = new BytesRef(new byte[] { 1, 2, 3, 4 }, 0, 4);
            BytesRef br2 = new BytesRef(new byte[] { 2, 3, 4, 5 }, 0, 4);
            Assert.True(br1.CompareTo(br2) < 0);

            br1 = new BytesRef(new byte[] { 1, 2, 3, 4 }, 1, 3);
            br2 = new BytesRef(new byte[] { 2, 3, 4, 5 }, 0, 3);
            Assert.True(br1.CompareTo(br2) == 0);

            br1 = new BytesRef(new byte[] { 1, 2, 3, 4 }, 0, 4);
            br2 = new BytesRef(new byte[] { 1, 3, 4 }, 0, 3);
            Assert.True(br1.CompareTo(br2) < 0);            
        }
        [Fact]
        public void testStringConstructor() {
            BytesRef br1 = new BytesRef("abcde");
            BytesRef br2 = new BytesRef(new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e' });
            Assert.Equal(br1, br2);
        }


    }
}