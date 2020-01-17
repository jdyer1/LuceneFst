using System;
using Xunit;

namespace Fst
{
    public class TestBytesStore
    {
        [Fact]
        public void test()
        {
            int iters = 1;
            int maxBytes = 20000;
            for (int iter = 0; iter < iters; iter++)
            {
                int numBytes = 1; //TODO: random 1-maxBytes-1
                byte[] expected = new byte[numBytes];
                int blockBits = 8; //TODO: random 8-15
                BytesStore bytes = new BytesStore(blockBits);

                int pos = 0;
                while (pos < numBytes)
                {
                    int op = 0; //TODO: random 0-7
                    switch (op)
                    {
                        case 0:
                            {
                                byte b = 0; //TODO: random 0-255
                                expected[pos++] = b;
                                bytes.writeByte(b);
                                break;
                            }
                            //TODO: case 1-7
                    }

                    Assert.Equal(pos, bytes.getPosition());

                    /*if (pos > 0)
                    { // TODO: && random().nextInt(50) == 17 
                        int upTo = Math.Min(pos, 100);
                        int len = 1;//TODO: random between 1 and  (upto-1)
                        bytes.truncate(pos - len);
                        pos -= 1;
                        Array.Clear(expected, pos, (pos + len));
                    }*/
                    if (pos > 0)
                    { // TODO:  && random().nextInt(200) == 17 
                        verify(bytes, expected, pos);
                    }

                }

                BytesStore bytesToVerify;
                { //TODO: if random.nextBoolean()
                    //TODO: save to disk, then reload
                } //todo: else
                bytesToVerify = bytes;
                verify(bytesToVerify, expected, numBytes);
            }

        }
        private void verify(BytesStore bytes, byte[] expected, int totalLength)
        {
            Assert.Equal(totalLength, bytes.getPosition());
            if (totalLength == 0)
            {
                return;
            }
            byte[] actual = new byte[totalLength];
            //TODO: if random.nextBoolean
            BytesReader r = bytes.getReverseReader();
            Assert.True(r.reversed());
            r.setPosition(totalLength - 1);
            r.readBytes(actual, 0, actual.Length);

            int start = 0;
            int end = totalLength - 1;
            while (start < end)
            {
                byte b = actual[start];
                actual[start] = actual[end];
                actual[end] = b;
                start++;
                end--;
            }

            for (int i = 0; i < totalLength; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }

            //TODO: random.nextBoolean() reversed/forward
            r = bytes.getForwardReader();

            if (totalLength > 1)
            {
                int numOps = 100; //TODO: random between 100-199
                for (int op = 0; op < numOps; op++)
                {
                    int numBytes = Math.Min(1000, totalLength - 1); //TODO: random between 1000-and totalLength-1
                    int pos = 0; //TODO: random between 0 and totalLength-numBytes-1.

                    byte[] temp = new byte[numBytes];
                    r.setPosition(pos);
                    Assert.Equal(pos, r.getPosition());
                    r.readBytes(temp, 0, temp.Length);
                    for (int i = 0; i < numBytes; i++)
                    {
                        byte expectedByte = expected[pos + i];
                        Assert.Equal(expectedByte, temp[i]);
                    }

                    int left = (int)r.getPosition();
                    int expectedPos = (int)(totalLength - r.getPosition());
                    Assert.Equal(expectedPos, r.getPosition());

                    if (left > 4)
                    {
                        int skipBytes = 1; // TODO: random betwen 0 and left-4-1.
                        int expectedInt = 0;
                        expectedPos += skipBytes;
                        expectedInt |= (expected[expectedPos++] & 0xFF) << 24;
                        expectedInt |= (expected[expectedPos++] & 0xFF) << 16;
                        expectedInt |= (expected[expectedPos++] & 0xFF) << 8;
                        expectedInt |= (expected[expectedPos++] & 0xFF);

                        r.skipBytes(skipBytes);
                        Assert.Equal(expectedInt, r.readInt());
                    }

                }
            }
        }
    }
}
