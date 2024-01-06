using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    internal static class StringExtensions
    {
        public static string Repeat(this string text, int count)
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < count; i++)
                stringBuilder.Append(text);

            return stringBuilder.ToString();
        }
    }
}
