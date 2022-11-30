using GiftyQueryLib.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftyQueryLib.Config
{
    /// <summary>
    /// Class to configure query builder
    /// </summary>
    public static class GiftyQueryConfig
    {
        /// <summary>
        /// Case Formatting for Database Objects (ex. Table, Column etc)
        /// </summary>
        public static CaseType CaseType { get; set; } = CaseType.Snake;

        /// <summary>
        /// Custom Formatting function using when CaseType is set to the "Custom"
        /// </summary>
        public static Func<string, string> CaseFormatterFunc { get; set; } = it => it;

        /// <summary>
        /// List of attribute types that mark field as a none-database<br/>
        /// Default: <b>NotMappedAttribute</b>
        /// </summary>
        public static HashSet<Type> NotMappedAttributes { get; } = new HashSet<Type>
        {
            typeof(NotMappedAttribute)
        };

        /// <summary>
        /// List of attribute types that mark field as a primary key field<br/>
        /// Default: <b>KeyAttribute</b>
        /// </summary>
        public static HashSet<Type> KeyAttributes { get; } = new HashSet<Type>
        {
            typeof(KeyAttribute)
        };

        /// <summary>
        /// List of attribute types that mark field as a foreign key field<br/>
        /// Default: <b>ForeignKeyAttribute</b>
        /// </summary>
        public static HashSet<Type> ForeignKeyAttributes { get; } = new HashSet<Type>
        {
            typeof(ForeignKeyAttribute)
        };
    }
}
