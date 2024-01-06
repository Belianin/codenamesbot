using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot;
public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> elements, int size)
    {
        var count = 0;
        var result = new List<T>(size);
        foreach (var element in elements)
        {
            result.Add(element);
            count++;
            if (count == size)
            {
                count = 0;
                yield return result;
                result = new List<T>(size);
            }
        }

        if (count != 0)
            yield return result;
    }
}