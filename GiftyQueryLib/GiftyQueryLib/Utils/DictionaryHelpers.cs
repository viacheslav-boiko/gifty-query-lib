namespace GiftyQueryLib.Utils
{
    public static class DictionaryHelpers
    {
        public static U? Value<T, U>(this Dictionary<T, U>? dict, T key) where T : notnull
        {
            if (dict is null)
                return default;

            var result = dict.TryGetValue(key, out var value);

            if (!result || value is null)
                return default;

            return value;
        }
    }
}
