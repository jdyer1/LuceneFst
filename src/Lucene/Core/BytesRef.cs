using System;
using System.Text;
using System.Diagnostics;

namespace Lucene.Core
{

    public sealed class BytesRef : IComparable, ICloneable, IEquatable<BytesRef>
    {
        public static readonly byte[] EMPTY_BYTES = new byte[0];
        public byte[] bytes;
        public int offset;
        public int length;

        public BytesRef()
        {
            bytes = EMPTY_BYTES;
        }

        public BytesRef(int capacity)
        {
            bytes = new byte[capacity];
        }

        public BytesRef(byte[] bytes, int offset, int length)
        {
            this.bytes = bytes;
            this.offset = offset;
            this.length = length;
            Debug.Assert(isValid());
        }

        public BytesRef(byte[] bytes)
        {
            this.bytes = bytes;
            this.offset = 0;
            this.length = bytes.Length;
            Debug.Assert(isValid());
        }

        public BytesRef(String s) : this(new byte[BytesRef.maxUTF8Length(s.Length)])
        {
            this.length = BytesRef.UTF16toUTF8(s, 0, s.Length, this.bytes, 0);
        }


        public int CompareTo(object obj)
        {
            if (!(obj is BytesRef))
            {
                throw new ArgumentException("Must compare to BytesRef.");
            }
            BytesRef that = (BytesRef)obj;
            int len = Math.Min(this.length, that.length);
            for (int i = 0; i < len; i++)
            {
                byte a = this.bytes[i + this.offset];
                byte b = that.bytes[i + that.offset];
                if (a > b)
                {
                    return 1;
                }
                else if (a < b)
                {
                    return -1;
                }
            }
            return this.length - that.length;
        }

        public Object Clone()
        {
            return new BytesRef(bytes, offset, length);
        }

        public bool Equals(BytesRef other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is BytesRef)
            {
                return this.bytesEquals((BytesRef)other);
            }
            return false;
        }

        public bool bytesEquals(BytesRef other)
        {
            if (this.length != other.length)
            {
                return false;
            }
            for (int i = 0; i < this.length; i++)
            {
                if (this.bytes[i + this.offset] != other.bytes[i + other.offset])
                {
                    return false;
                }
            }
            return true;
        }

        ///TODO:  Lucene's BytesRef 8.x uses MurmurHash3 with
        ///       a unique seed based on the system clock.
        public override int GetHashCode()
        {
            int prime = 13;
            int result = 0;
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                result = prime * result + bytes[i];
            }
            return result;
        }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                if (i > offset)
                {
                    sb.Append(' ');
                }
                sb.Append(bytes[i].ToString("X"));
            }
            sb.Append(']');
            return sb.ToString();
        }
        private static readonly long UNI_MAX_BMP = 0x0000FFFF;
        private static readonly long HALF_MASK = 0x3FFL;
        ///Taken from: org.apache.lucene.util.UnicodeUtil#UTF8toUTF16
        public String utf8ToString()
        {
            char[] outChar = new char[length];
            int stringOffset = 0;
            int byteOffset = this.offset;
            int limit = byteOffset + length;
            while (byteOffset < limit)
            {
                int b = bytes[byteOffset++] & 0xff;
                if (b < 0xc0)
                {
                    Debug.Assert(b < 0x80);
                    outChar[stringOffset++] = (char)b;
                }
                else if (b < 0xe0)
                {
                    outChar[stringOffset++] = (char)(((b & 0x1f) << 6) + (bytes[byteOffset++] & 0x3f));
                }
                else if (b < 0xf0)
                {
                    outChar[stringOffset++] = (char)(((b & 0xf) << 12) + ((bytes[byteOffset] & 0x3f) << 6) + (bytes[byteOffset + 1] & 0x3f));
                    byteOffset += 2;
                }
                else
                {
                    Debug.Assert(b < 0xf8, "b = 0x" + b.ToString("X"));
                    int ch = ((b & 0x7) << 18) + ((bytes[byteOffset] & 0x3f) << 12) + ((bytes[byteOffset + 1] & 0x3f) << 6) + (bytes[byteOffset + 2] & 0x3f);
                    offset += 3;
                    if (ch < UNI_MAX_BMP)
                    {
                        outChar[stringOffset++] = (char)ch;
                    }
                    else
                    {
                        int chHalf = ch - 0x0010000;
                        outChar[stringOffset++] = (char)((chHalf >> 10) + 0xD800);
                        outChar[stringOffset++] = (char)((chHalf & HALF_MASK) + 0xDC00);
                    }
                }
            }
            return new String(outChar, 0, stringOffset);
        }

        private static readonly int MAX_UTF8_BYTES_PER_CHAR = 3;
        ///Taken from: org.apache.lucene.util.UnicodeUtil#maxUTF8Length
        private static int maxUTF8Length(int utf16Length)
        {
            checked
            {
                return utf16Length * MAX_UTF8_BYTES_PER_CHAR;
            }
        }
        private static readonly int UNI_SUR_HIGH_START = 0xD800;
        private static readonly int UNI_SUR_LOW_START = 0xDC00;
        private static readonly int HALF_SHIFT = 10;
        private static readonly long MIN_SUPPLEMENTARY_CODE_POINT = 0x010000;
        private static readonly long SURROGATE_OFFSET = MIN_SUPPLEMENTARY_CODE_POINT - (UNI_SUR_HIGH_START << HALF_SHIFT) - UNI_SUR_LOW_START;

        /// Taken from org.apache.lucene.util.UnicodeUtil#UTF16toUTF8 .
        /// Populates the UTF-8 bytes in "bOut".
        /// <returns>the final output offset (outOffset + number of bytes written)</returns>
        private static int UTF16toUTF8(String s, int offset, int length, byte[] bOut, int outOffset)
        {
            int end = offset + length;

            int upto = outOffset;
            for (int i = offset; i < end; i++)
            {
                int code = (int)s[i];

                if (code < 0x80)
                    bOut[upto++] = (byte)code;
                else if (code < 0x800)
                {
                    bOut[upto++] = (byte)(0xC0 | (code >> 6));
                    bOut[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    bOut[upto++] = (byte)(0xE0 | (code >> 12));
                    bOut[upto++] = (byte)(0x80 | ((code >> 6) & 0x3F));
                    bOut[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && (i < end - 1))
                    {
                        int utf32 = (int)s[i + 1];
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (int)((code << 10) + utf32 + SURROGATE_OFFSET);
                            i++;
                            bOut[upto++] = (byte)(0xF0 | (utf32 >> 18));
                            bOut[upto++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            bOut[upto++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            bOut[upto++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    bOut[upto++] = (byte)0xEF;
                    bOut[upto++] = (byte)0xBF;
                    bOut[upto++] = (byte)0xBD;
                }
            }

            return upto;
        }

        public bool isValid()
        {
            if (bytes == null)
            {
                throw new ArgumentException("bytes is null");
            }
            if (length < 0)
            {
                throw new ArgumentException("length is negative: " + length);
            }
            if (length > bytes.Length)
            {
                throw new ArgumentException("length is out of bounds: " + length + ",bytes.length=" + bytes.Length);
            }
            if (offset < 0)
            {
                throw new ArgumentException("offset is negative: " + offset);
            }
            if (offset > bytes.Length)
            {
                throw new ArgumentException("offset out of bounds: " + offset + ",bytes.length=" + bytes.Length);
            }
            if (offset + length < 0)
            {
                throw new ArgumentException("offset+length is negative: offset=" + offset + ",length=" + length);
            }
            if (offset + length > bytes.Length)
            {
                throw new ArgumentException("offset+length out of bounds: offset=" + offset + ",length=" + length + ",bytes.Length=" + bytes.Length);
            }
            return true;
        }
    }
}