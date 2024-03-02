using System.Text;

namespace ChaosUtil.Debug
{
    /// <summary> Provides utility functions on string objects. </summary>
    public static class StringUtils
    {
        /// <summary> Returns a string consisting of the provided string repeated a given number of times. </summary>
        /// <param name="str"> The string to be repeated. </param>
        /// <param name="num"> The number of times the string shall be repeated. </param>
        /// <returns> The resulting string. </returns>
        public static string Repeat(this string str, int num)
        {
            StringBuilder bldr = new StringBuilder(str.Length * num);
            for (int i = 0; i < num; i++)
                bldr.Append(str);
            return bldr.ToString();
        }

        /// <summary> Returns a string where all control characters are escaped. </summary>
        /// <param name="source"> The string to be escaped. </param>
        /// <returns> The escaped string. </returns>
        public static string EscapeControlCharacters(this string source)
        {
            StringBuilder newText = new StringBuilder(source.Length);
            foreach (char c in source)
                switch (c)
                {
                    case '\a': newText.Append("\\a"); break;
                    case '\b': newText.Append("\\b"); break;
                    case '\f': newText.Append("\\f"); break;
                    case '\n': newText.Append("\\n"); break;
                    case '\r': newText.Append("\\r"); break;
                    case '\t': newText.Append("\\t"); break;
                    case '\v': newText.Append("\\v"); break;
                    default: newText.Append(c); break;
                }
            return newText.ToString();
        }

        /// <summary>
        ///     Returns a string representing a floating point format consisting
        ///     of a given number of integer digits and fractional digits.
        /// </summary>
        /// <param name="integerDigits"> The minimum number of integer digits. </param>
        /// <param name="fractionalDigits"> The exact number of fractional digits. </param>
        /// <returns> The format string. </returns>
        public static string GetFloatFormat(int integerDigits, int fractionalDigits)
            => $"{new string('0', integerDigits)}.{new string('0', fractionalDigits)}";
    }
}
