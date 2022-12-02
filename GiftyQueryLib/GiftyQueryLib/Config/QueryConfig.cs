using GiftyQueryLib.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftyQueryLib.Config
{
    /// <summary>
    /// Class to configure settings of query builder
    /// </summary>
    public static class QueryConfig
    {
        /// <summary>
        /// Case Formatting for Database Objects
        /// Default: Snake
        /// </summary>
        public static CaseType CaseType { get; set; } = CaseType.Snake;

        /// <summary>
        /// Custom Formatting function using when CaseType is set to the "Custom"
        /// </summary>
        public static Func<string, string> CaseFormatterFunc { get; set; } = it => it;

        /// <summary>
        /// If flag is <b>true</b>, system will take database column name that set in Column attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database column name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public static bool UseNamesProvidedInColumnAttribute { get; set; } = false;

        /// <summary>
        /// If flag is <b>true</b>, system will take database table name that set in Table attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database table name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public static bool UseNamesProvidedInTableAttribute { get; set; } = false;

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

        /// <summary>
        /// Scheme name for databases that support schemes infrastructure
        /// Default: <b>public</b>
        /// </summary>
        public static string Scheme { get; set; } = "public";

        /// <summary>
        /// Column access for query builder
        /// Default: <b>{0}.{1}.{2}</b> (0 - scheme, 1 - table, 2 - column)
        /// </summary>
        public static string ColumnAccessFormat { get; set; } =  "{0}.{1}.{2}";
    }
}
