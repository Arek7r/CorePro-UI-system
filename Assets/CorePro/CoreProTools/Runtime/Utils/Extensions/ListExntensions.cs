using System.Collections.Generic;

namespace CorePro.Utils.Extensions
{
    public static class ListExtensions
    {
        // Adds elements from the array to the existing list without new allocation List<T>
        public static void AddRangeNoAlloc<T>(this List<T> list, T[] array)
        {
            for (int i = 0; i < array.Length; i++)
                list.Add(array[i]);
        }
        
        public static void ToListNoAlloc<T>(this IEnumerable<T> source, List<T> targetList)
        {
            targetList.Clear();
            foreach (var item in source)
                targetList.Add(item);
        }
    }
}