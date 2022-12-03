namespace GiftyQueryLib.Utils
{
    public static class Constants
    {
        public static HashSet<Type> TypesToStringCast => new()
        {
            typeof(string), typeof(char), typeof(Guid), typeof(DateTime), typeof(TimeSpan)
        };

        public static HashSet<Type> NumericTypes => new()
        {
            typeof(int), typeof(long), typeof(short), typeof(double), typeof(decimal), 
            typeof(float), typeof (byte), typeof(sbyte), typeof(uint), typeof(ulong)
        };
    }
}
