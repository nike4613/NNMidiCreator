using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiParser
{
    public static class Extensions
    {
        public static string ToString<T>(this List<T> list)
        {
            string outp = "[";

            foreach (var el in list)
            {
                if (typeof(T).IsPrimitive)
                    outp += el.ToString() + ",";
                else
                    outp += "{" + el.ToString() + "},";
            }

            return outp.Substring(0,outp.Length-1) + "]";
        }

        public static string ToString<T>(this T[] list)
        {
            string outp = "[";

            foreach (var el in list)
            {
                if (typeof(T).IsPrimitive)
                    outp += el.ToString() + ",";
                else
                    outp += "{" + el.ToString() + "},";
            }

            return outp.Substring(0, outp.Length - 1) + "]";
        }

        public static string Repeat(this string self, int times)
        {
            string text = "";

            while (times-- > 0)
            {
                text += self;
            }

            return text;
        }
    }
}
