using System.Text;

namespace GiftyQueryLib.Utils
{
    public static class StringHelpers
    {
        public static string TrimEndComma(this string str, int count = 2) 
            => str.EndsWith(", ") ? str.Remove(str.Length - count) : str;

        public static string TrimEndComma(this StringBuilder sb, int count = 2)
            => TrimEndComma(sb.ToString(), count);
    }
}
