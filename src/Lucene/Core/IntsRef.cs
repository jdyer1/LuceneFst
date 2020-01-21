using System;
using System.Text;
using System.Diagnostics;

namespace Lucene.Core
{

    public sealed class IntsRef : IComparable, ICloneable, IEquatable<IntsRef>
    {
        public static readonly int[] EMPTY_INTS = new int[0];
        public int[] ints;
        public int offset;
        public int length;

        public IntsRef()
        {
            ints = EMPTY_INTS;
        }

        public IntsRef(int capacity)
        {
            ints = new int[capacity];
        }

        public IntsRef(int[] ints, int offset, int length)
        {
            this.ints = ints;
            this.offset = offset;
            this.length = length;
            Debug.Assert(isValid());
        }


        public int CompareTo(object obj)
        {
            return 0; //TODO
        }

        public Object Clone()
        {
            return new IntsRef(ints, offset, length);
        }

        public bool Equals(IntsRef other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is IntsRef)
            {
                return this.intsEquals((IntsRef)other);
            }
            return false;
        }

        public bool intsEquals(IntsRef other)
        {
            if (this.length != other.length)
            {
                return false;
            }
            for (int i = 0; i < this.length; i++)
            {
                if (this.ints[i + this.offset] != other.ints[i + other.offset])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 0;
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                result = prime * result + ints[i];
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
                sb.Append(ints[i].ToString("X"));
            }
            sb.Append(']');
            return sb.ToString();
        }


        public bool isValid()
        {
            if (ints == null)
            {
                throw new ArgumentException("ints is null");
            }
            if (length < 0)
            {
                throw new ArgumentException("length is negative: " + length);
            }
            if (length > ints.Length)
            {
                throw new ArgumentException("length is out of bounds: " + length + ",ints.length=" + ints.Length);
            }
            if (offset < 0)
            {
                throw new ArgumentException("offset is negative: " + offset);
            }
            if (offset > ints.Length)
            {
                throw new ArgumentException("offset out of bounds: " + offset + ",ints.length=" + ints.Length);
            }
            if (offset + length < 0)
            {
                throw new ArgumentException("offset+length is negative: offset=" + offset + ",length=" + length);
            }
            if (offset + length > ints.Length)
            {
                throw new ArgumentException("offset+length out of bounds: offset=" + offset + ",length=" + length + ",ints.Length=" + ints.Length);
            }
            return true;
        }
    }
}