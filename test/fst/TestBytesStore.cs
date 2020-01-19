using System.Runtime.InteropServices.WindowsRuntime;
using System;
using Xunit;
using TestUtil;
using Serilog;

namespace Fst
{
    public class TestBytesStore
    {
        public static readonly ILogger log = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
        private const int seed = 1;
        private readonly RandomTester r = new RandomTester(seed);

        [Fact]
        public void test()
        {
            int iters = r.intBetween(10, 20);
            int maxBytes = 20000;
            for (int iter = 0; iter < iters; iter++)
            {
                int numBytes = r.intBetween(1, maxBytes);
                byte[] expected = new byte[numBytes];
                int blockBits = r.intBetween(8, 15);
                BytesStore bytes = new BytesStore(blockBits);
                log.Information("TEST: iter={iter} numBytes={numBytes}, blockBits={blockBits}", iter, numBytes, blockBits);

                int pos = 0;
                while (pos < numBytes)
                {
                    int op = r.intBetween(0, 7);
                    log.Information("> cycle pos={pos} op={op}", pos, op);
                    switch (op)
                    {
                        case 0:
                            {
                                byte b = (byte)r.intBetween(0, 255);
                                log.Information(">> writeByte b={b}", b);
                                expected[pos++] = b;
                                bytes.writeByte(b);
                                break;
                            }
                        case 1:
                            {
                                int len = r.r().Next(Math.Min(numBytes - pos, 100));
                                byte[] temp = new byte[len];
                                r.bytes(temp);
                                log.Information(">> writeBytes len={len} bytes={bytes}", len, bytes);
                                Array.Copy(temp, 0, expected, pos, temp.Length);
                                bytes.writeBytes(temp, 0, temp.Length);
                                pos += len;
                                break;
                            }
                        case 2:
                            {
                                if (pos > 4)
                                {
                                    int x = r.r().Next();
                                    int randomPos = r.r().Next(pos - 4);
                                    log.Information(">> abs writeInt pos={pos} x={x}", randomPos, x);
                                    bytes.writeInt(randomPos, x);
                                    expected[randomPos++] = (byte)(x >> 24);
                                    expected[randomPos++] = (byte)(x >> 16);
                                    expected[randomPos++] = (byte)(x >> 8);
                                    expected[randomPos++] = (byte)x;
                                }
                                break;
                            }
                        case 3:
                            {
                                if (pos > 1)
                                {
                                    int len = r.intBetween(2, Math.Min(100, pos));
                                    int start;
                                    if (len == pos)
                                    {
                                        start = 0;
                                    }
                                    else
                                    {
                                        start = r.r().Next(pos - len);
                                    }
                                    int end = start + len - 1;
                                    log.Information(">> reverse start={start} end={end} len={len}, pos={pos}", start, end, len, pos);
                                    bytes.reverse(start, end);

                                    while (start <= end)
                                    {
                                        byte b = expected[end];
                                        expected[end] = expected[start];
                                        expected[start] = b;
                                        start++;
                                        end--;
                                    }
                                }
                                break;
                            }
                        case 4:
                            {
                                if (pos > 2)
                                {
                                    int randomPos = r.r().Next(pos - 1);
                                    int len = r.intBetween(1, Math.Min(pos - randomPos - 1, 100));
                                    byte[] temp = new byte[len];
                                    r.r().NextBytes(temp);
                                    log.Information(">> abs writeBytes pos={randomPos} len={len} bytes={bytes}", randomPos, len, temp);
                                    Array.Copy(temp, 0, expected, randomPos, temp.Length);
                                    bytes.writeBytes(randomPos, temp, 0, temp.Length);
                                }
                                break;
                            }
                        case 5:
                            {
                                if (pos > 1)
                                {
                                    int src = r.r().Next(pos - 1);
                                    int dest = r.intBetween(src + 1, pos - 1);
                                    int len = r.intBetween(1, Math.Min(300, pos - dest));
                                    log.Information(">> copyBytes src={src} dest={dest} len={len}", src, dest, len);
                                    Array.Copy(expected, src, expected, dest, len);
                                    bytes.copyBytes(src, dest, len);
                                }
                                break;
                            }
                        case 6:
                            {
                                int len = r.r().Next(Math.Min(100, numBytes - pos));
                                log.Information(">> skip len={len}", len);
                                pos += len;
                                bytes.skipBytes(len);
                                if (len > 0)
                                {
                                    byte[] zeros = new byte[len];
                                    bytes.writeBytes(pos - len, zeros, 0, len);
                                }
                                break;
                            }
                        case 7:
                            {
                                if (pos > 0)
                                {
                                    int dest = r.r().Next(pos);
                                    byte b = (byte)r.r().Next(256);
                                    log.Information(">> abs writeByte dest={len} byte={b}", dest, b);
                                    expected[dest] = b;
                                    bytes.writeByte(dest, b);
                                }
                                break;
                            }
                    }

                    Assert.Equal(pos, bytes.getPosition());

                    if (pos > 0 && r.r().Next(50) == 17)
                    {
                        int len = r.intBetween(1, Math.Min(pos, 100));
                        log.Debug(">> truncate to size: {newSize}", (pos - len));
                        bytes.truncate(pos - len);
                        pos -= len;
                        Array.Clear(expected, pos, len);
                    }
                    /*if (pos > 0 && r.r().Next(200) == 17)
                    {
                        verify(bytes, expected, pos);
                    }*/

                }

                BytesStore bytesToVerify;
                if (r.boolean())
                {
                    //TODO: save to disk, then reload
                    bytesToVerify = bytes;
                }
                else
                {
                    bytesToVerify = bytes;
                }
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
            if (r.boolean())
            {
                log.Debug(">>> verify bulk: reversed");
                BytesReader br = bytes.getReverseReader();
                Assert.True(br.reversed());
                br.setPosition(totalLength - 1);
                br.readBytes(actual, 0, actual.Length);

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
            }
            else
            {
                log.Debug(">>> verify bulk: forward");
                BytesReader br = bytes.getForwardReader();
                Assert.False(br.reversed());
                br.readBytes(actual, 0, actual.Length);
            }

            for (int i = 0; i < totalLength; i++)
            {
                Assert.True(expected[i] == actual[i], "byte @ index=" + i + " expected: " + expected[i] + " actual: " + actual[i]);
            }

            Boolean reversed = r.boolean();
            BytesReader brOps;
            if (reversed)
            {
                log.Debug(">>> ops: reversed");
                brOps = bytes.getReverseReader();
            }
            else
            {
                log.Debug(">>> ops: forward");
                brOps = bytes.getForwardReader();
            }
            if (totalLength > 1)
            {
                int numOps = r.intBetween(100, 200);
                for (int op = 0; op < numOps; op++)
                {
                    int numBytes = r.r().Next(Math.Min(1000, totalLength - 1));
                    int pos;
                    if (reversed)
                    {
                        pos = r.intBetween(numBytes, totalLength - 1);
                    }
                    else
                    {
                        pos = r.r().Next(totalLength - numBytes);
                    }

                    log.Debug(">>> op iter={op} reversed={reversed}  numBytes={numBytes} pos={pos}", op, reversed, numBytes, pos);

                    byte[] temp = new byte[numBytes];
                    brOps.setPosition(pos);
                    Assert.Equal(pos, brOps.getPosition());
                    brOps.readBytes(temp, 0, temp.Length);
                    for (int i = 0; i < numBytes; i++)
                    {
                        byte expectedByte;
                        if (reversed)
                        {
                            expectedByte = expected[pos - i];
                        }
                        else
                        {
                            expectedByte = expected[pos + i];
                        }
                        Assert.True(expectedByte == temp[i], "byte @ index=" + i);
                    }

                    int left;
                    int expectedPos;
                    if (reversed)
                    {
                        expectedPos = pos - numBytes;
                        left = (int)brOps.getPosition();
                    }
                    else
                    {
                        expectedPos = pos + numBytes;
                        left = (int) (totalLength - brOps.getPosition());
                    }

                    Assert.Equal(expectedPos, brOps.getPosition());

                    if (left > 4)
                    {
                        int skipBytes = r.r().Next(left - 4);
                        int expectedInt = 0;
                        if (reversed)
                        {
                            expectedPos -= skipBytes;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 24;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 16;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 8;
                            expectedInt |= (expected[expectedPos--] & 0xFF);
                        }
                        else
                        {
                            expectedPos += skipBytes;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 24;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 16;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 8;
                            expectedInt |= (expected[expectedPos++] & 0xFF);
                        }
                        log.Debug(">>> skip numBytes={}", skipBytes);
                        brOps.skipBytes(skipBytes);

                        int actualInt = brOps.readInt();
                        log.Debug(">>> readInt={actualInt}", actualInt);

                        Assert.Equal(expectedInt, actualInt);
                    }

                }
            }
        }
    }
}
