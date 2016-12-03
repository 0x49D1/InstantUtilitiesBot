using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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

        public static string RemoveCommandPart(this string commandString)
        {
            return Regex.Replace(commandString, @"\/[^\s]+", "");
        }

        public static int ToUnixTimestamp(this DateTime value)
        {
            return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }

        public static bool StartsWithOrdinalIgnoreCase(this string text, string startsWith)
        {
            string[] values = startsWith.Split(';');
            foreach (var v in values)
            {
                if (text.StartsWith(v, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T obj in source)
                action(obj);
        }

        public static string Hash(this string input, Type t)
        {
            StringBuilder hash = new StringBuilder();
            if (t == typeof(MD5))
            {
                using (MD5 md5 = MD5.Create())
                    ForEach<byte>((IEnumerable<byte>)md5.ComputeHash(Encoding.UTF8.GetBytes(input)), (Action<byte>)(c => hash.Append(c.ToString("x2"))));
            }
            else if (t == typeof(SHA1))
            {
                using (SHA1 shA1 = SHA1.Create())
                    ForEach<byte>((IEnumerable<byte>)shA1.ComputeHash(Encoding.UTF8.GetBytes(input)), (Action<byte>)(c => hash.Append(c.ToString("x2"))));
            }
            else if (t == typeof(SHA256))
            {
                using (SHA256 shA256 = SHA256.Create())
                    ForEach<byte>((IEnumerable<byte>)shA256.ComputeHash(Encoding.UTF8.GetBytes(input)), (Action<byte>)(c => hash.Append(c.ToString("x2"))));
            }
            else if (t == typeof(SHA512))
            {
                using (SHA512 shA512 = SHA512.Create())
                    ForEach<byte>((IEnumerable<byte>) shA512.ComputeHash(Encoding.UTF8.GetBytes(input)),
                        (Action<byte>) (c => hash.Append(c.ToString("x2"))));
            }
            else
            {
                throw new AmbiguousMatchException("Type not defined");
            }
            return hash.ToString();
        }

        public static string Hash<T>(this string input)
        {
            return Hash(input, typeof(T));
        }
    }
}
