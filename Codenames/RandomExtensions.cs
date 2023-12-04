namespace Codenames
{
    public static class RandomExtensions
    {
        public static IList<T> Pick<T>(this Random random, IList<T> elements, int count)
        {
            if (count > elements.Count)
                throw new ArgumentException(nameof(count));

            var indexes = new HashSet<int>(count);
            for (var i = 0; i < count; i++)
            {
                var index = random.Next(elements.Count);
                while (indexes.Contains(index))
                    index = random.Next(elements.Count);
                indexes.Add(index);
            }

            return indexes.Select(x => elements[x]).ToList();
        }
    }
}