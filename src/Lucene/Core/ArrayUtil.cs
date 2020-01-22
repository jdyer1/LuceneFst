using System;

namespace Lucene.Core {
    public static class ArrayUtil {
        public static T[] grow<T>(T[] arr, int minSize) {
            //TODO: Lucene uses complex logic to determine new size
            int sizeToGrow = Math.Max(minSize, arr.Length * 2);
            T[] newArr = new T[sizeToGrow];
            Array.Copy(arr, newArr, arr.Length);
            return newArr;
        }
    }
}