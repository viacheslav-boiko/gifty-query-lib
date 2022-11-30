namespace GiftyQueryLib.Utils
{
    public static class Constants
    {
        public static List<Type> TypesToStringCast => new()
        {
            typeof(string), typeof(char), typeof(Guid), typeof(DateTime), typeof(TimeSpan)
        };
    }
}
