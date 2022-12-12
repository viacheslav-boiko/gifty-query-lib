using System.Collections;
using System.Reflection;

namespace GiftyQueryLib.Utils
{
    public static class PropertyHelpers
    {
        public static bool IsEnumerable(this PropertyInfo? property) => property is not null && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string);
        public static bool IsArray(this PropertyInfo? property) => property is not null && typeof(Array).IsAssignableFrom(property.PropertyType);
        public static bool IsCollection(this PropertyInfo? property) => IsEnumerable(property) || IsArray(property);
        public static Type? GetGenericArg(this PropertyInfo? property) => property is not null
            ? IsArray(property) ? property.PropertyType : property.PropertyType.GenericTypeArguments.FirstOrDefault()
            : null;
    }
}
