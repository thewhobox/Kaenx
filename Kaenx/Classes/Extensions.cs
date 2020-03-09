using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    static class Extensions
    {
        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> collection, Func<TSource, TKey> keySelector)
        {
            List<TSource> sorted = collection.OrderBy(keySelector).ToList();
            //for (int i = 0; i < sorted.Count(); i++)
            //    collection.Move(collection.IndexOf(sorted[i]), i);
            collection.Clear();
            foreach (var sortedItem in sorted)
                collection.Add(sortedItem);
        }
    }
}
