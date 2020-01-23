using System;

namespace Lucene.Core
{
    public static class ArrayUtil
    {
        public static T[] grow<T>(T[] arr, int minSize)
        {

            int sizeToGrow = oversize(minSize, arr.Length);
            T[] newArr = new T[sizeToGrow];
            Array.Copy(arr, newArr, arr.Length);
            return newArr;
        }
        
        ///TODO: Lucene uses complex logic to determine new size
        public static int oversize(int minTargetSize, int bytesPerElement)
        {
            return Math.Max(minTargetSize, bytesPerElement * 2);
        }
    }
}