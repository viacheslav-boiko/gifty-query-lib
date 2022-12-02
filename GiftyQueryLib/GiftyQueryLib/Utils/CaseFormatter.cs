﻿using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using System.Text;

namespace GiftyQueryLib.Utils
{
    public static class CaseFormatter
    {
        /// <summary>
        /// Converts string into formatted case
        /// </summary>
        /// <param name="text">Source text</param>
        /// <returns>Formatted string in a specified case</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToCaseFormat(this string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var caseType = QueryConfig.CaseType;

            return caseType switch
            {
                CaseType.Snake => ToSnakeCase(text),
                CaseType.Camel => ToCamelCase(text),
                CaseType.Custom => QueryConfig.CaseFormatterFunc.Invoke(text),
                _ => text,
            };
        }

        /// <summary>
        /// Converts type name into formatted case
        /// </summary>
        /// <param name="type">Source type</param>
        /// <returns>Formatted string in a specified case</returns>
        public static string ToCaseFormat(this Type type)
        {
            return ToCaseFormat(type.Name);
        }

        private static string ToCamelCase(string text)
        {
            if (text.Length < 2)
                return text;

            var sb = new StringBuilder();
            sb.Append(char.ToUpperInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                
                if (c == '_')
                {
                    i++;
                    sb.Append(char.ToUpperInvariant(text[i]));
                }

                sb.Append(c);
            }
            return sb.ToString();
        }

        private static string ToSnakeCase(string text)
        {
            if (text.Length < 2)
                return text;

            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                if (char.IsUpper(c))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
