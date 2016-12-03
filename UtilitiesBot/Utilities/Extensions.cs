using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UtilitiesBot.Utilities
{
    public static class Extensions
    {
        public static string ToJson(this object obj, Formatting format = Formatting.Indented)
        {
            return JsonConvert.SerializeObject(obj, format);
        }

        public static string RemoveCommandPart(this string commandString) {
            return Regex.Replace(commandString, @"\/[^\s]+", "");
        }

        public static int ToUnixTimestamp(this DateTime value)
        {
            return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }

        public static bool StartsWithOrdinalIgnoreCase(this string text, string startsWith) {
            string[] values = startsWith.Split(';');
            foreach (var v in values) {
                if (text.StartsWith(v, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
