using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace landrope.common
{
    internal static class ArrayExtensions
    {
        public static T[] Add<T>(this T[] arr, T item)
        {
            var lst = arr.ToList();
            lst.Add(item);
            return lst.ToArray();
        }

        public static T[] Del<T>(this T[] arr, T item)
        {
            var lst = arr.ToList();
            if (lst.Remove(item))
                return lst.ToArray();
            return arr;
        }

        public static T[] Del<T>(this T[] arr, Predicate<T> predicate)
        {
            if (predicate == null)
                return arr;
            var item = arr.FirstOrDefault(t => predicate(t));
            if (item == null)
                return arr;

            var lst = arr.ToList();
            if (lst.Remove(item))
                return lst.ToArray();
            return arr;
        }

        public static T[] AddOrReplace<T>(this T[] arr, Predicate<T> predicate, T replacer)
        {
            if (predicate == null)
                return arr;
            var idx = arr.ToList().FindIndex(t => predicate(t));
            if (idx == -1)
                return arr.Add(replacer);
            arr[idx] = replacer;
            return arr;
        }
    }

    public static class StringExtensions
    {
        public static string ToEscape(this string st)
        {
            string rs = st;
            string[] toReplace = new string[] { "\\", ".", ",", "{", "}", "[", "]", "$", ":", ";", "'", "/", "\"", "(", ")", "?" };
            foreach (string s in toReplace)
                rs = rs.Replace(s, @$"\{s}");
            return rs;
        }

        public static string ToEnumJenisProses(this string tfind)
            => tfind?.ToLower()?.Trim() switch
            {
                "standar" => "0",
                "claim" => "2",
                "bintang" => "4",
                _ => tfind
            };
    }
}
