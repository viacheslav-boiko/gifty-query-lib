namespace GiftyQueryLib.Translators.Models
{
    /// <summary>
    /// Parsed Selector Data
    /// </summary>
    public class SelectorData
    {
        /// <summary>
        /// Result string after parsing selector
        /// </summary>
        public string? Result { get; init; }

        /// <summary>
        /// Extra data
        /// </summary>
        public dynamic? ExtraData { get; init; }
    }
}
