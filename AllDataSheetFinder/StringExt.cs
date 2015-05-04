using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public static class StringExt
    {
        public static string RemoveAll(this string obj, Predicate<char> match)
        {
            List<char> chars = obj.ToList();
            chars.RemoveAll(match);
            return string.Concat(chars);
        }
    }
}
