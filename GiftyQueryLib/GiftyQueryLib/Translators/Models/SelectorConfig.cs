namespace GiftyQueryLib.Translators.Models
{
    public class SelectorConfig
    {
        public bool SelectAll { get; init; } = false;
        public bool AllowMember { get; init; } = true;
        public bool AllowMethodCall { get; init; } = true;
        public bool AllowBinary { get; init; } = true;
        public bool AllowConstant { get; init; } = true;
        public bool UseAliases { get; init; } = true;
    }
}
