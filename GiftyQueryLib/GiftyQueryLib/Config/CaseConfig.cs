using GiftyQueryLib.Enums;

namespace GiftyQueryLib.Config
{
    /// <summary>
    /// Case Formatter Configuration class
    /// </summary>
    public class CaseConfig
    {
        /// <summary>
        /// Case Formatting for Database Objects<br/>
        /// Default: <b>Snake</b>
        /// </summary>
        public CaseType CaseType { get; set; } = CaseType.Snake;

        /// <summary>
        /// Custom Formatting function using when CaseType is set to the "Custom"
        /// </summary>
        public Func<string, string> CaseFormatterFunc { get; set; } = it => it;
    }
}
