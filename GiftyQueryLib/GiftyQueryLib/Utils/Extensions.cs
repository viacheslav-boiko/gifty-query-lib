using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using System.Collections;
using System.Reflection;
using System.Text;

namespace GiftyQueryLib.Utils
{
    /// <summary>
    /// Class for extension methods
    /// </summary>
    public static class Extensions
    {
        #region Case Formatter Extentions

        public static string ToCaseFormat(this string text, CaseConfig config)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var caseType = config.CaseType;

            return caseType switch
            {
                CaseType.Snake => ToSnakeCase(text),
                CaseType.Camel => ToCamelCase(text),
                CaseType.None => config.CaseFormatterFunc.Invoke(text),
                _ => text,
            };
        }

        public static string ToCaseFormat(this Type type, CaseConfig config)
        {
            return ToCaseFormat(type.Name, config);
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

        #endregion

        #region Property Extensions

        public static bool IsEnumerable(this PropertyInfo? property) => property is not null && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string);
        public static bool IsArray(this PropertyInfo? property) => property is not null && typeof(Array).IsAssignableFrom(property.PropertyType);
        public static bool IsCollection(this PropertyInfo? property) => IsEnumerable(property) || IsArray(property);
        public static Type? GetGenericArg(this PropertyInfo? property) => property is not null
            ? IsArray(property) ? property.PropertyType : property.PropertyType.GenericTypeArguments.FirstOrDefault()
            : null;

        #endregion

        #region Dictionary Extensions

        public static U? Value<T, U>(this Dictionary<T, U>? dict, T key) where T : notnull
        {
            if (dict is null)
                return default;

            var result = dict.TryGetValue(key, out var value);

            if (!result || value is null)
                return default;

            return value;
        }

        #endregion

        #region String Extensions

        public static string TrimEndComma(this string str, int count = 2)
          => str.EndsWith(", ") ? str.Remove(str.Length - count) : str;

        public static string TrimEndComma(this StringBuilder sb, int count = 2)
            => TrimEndComma(sb.ToString(), count);

        #endregion
    }
}
