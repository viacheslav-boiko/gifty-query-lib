using GiftyQueryLib.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftyQueryLib.Config
{
    /// <summary>
    /// Class to configure settings of PostgreSQL query builder
    /// </summary>
    public class PostgreSqlConfig
    {
        /// <summary>
        /// Case Formatting for Database Objects
        /// Default: Snake
        /// </summary>
        public CaseType CaseType { get; init; } = CaseType.Snake;

        /// <summary>
        /// Custom Formatting function using when CaseType is set to the "Custom"
        /// </summary>
        public Func<string, string> CaseFormatterFunc { get; init; } = it => it;

        /// <summary>
        /// If flag is <b>true</b>, system will take database column name that set in Column attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database column name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public bool UseNamesProvidedInColumnAttribute { get; init; } = false;

        /// <summary>
        /// If flag is <b>true</b>, system will take database table name that set in Table attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database table name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public bool UseNamesProvidedInTableAttribute { get; init; } = false;

        /// <summary>
        /// List of attribute types that mark field as a none-database<br/>
        /// Default: <b>NotMappedAttribute</b>
        /// </summary>
        public HashSet<Type> NotMappedAttributes { get; } = new HashSet<Type>
        {
            typeof(NotMappedAttribute)
        };

        /// <summary>
        /// List of attribute types that mark field as a primary key field<br/>
        /// Default: <b>KeyAttribute</b>
        /// </summary>
        public HashSet<Type> KeyAttributes { get; } = new HashSet<Type>
        {
            typeof(KeyAttribute)
        };

        /// <summary>
        /// List of attribute types that mark field as a foreign key field<br/>
        /// Default: <b>ForeignKeyAttribute</b>
        /// </summary>
        public HashSet<Type> ForeignKeyAttributes { get; } = new HashSet<Type>
        {
            typeof(ForeignKeyAttribute)
        };

        /// <summary>
        /// Scheme name for databases that support schemes infrastructure
        /// Default: <b>public</b>
        /// </summary>
        public string Scheme { get; init; } = "public";

        /// <summary>
        /// Column access for query builder
        /// Default: <b>{0}.{1}.{2}</b> (0 - scheme, 1 - table, 2 - column)
        /// </summary>
        public string ColumnAccessFormat { get; init; } = "{0}.{1}.{2}";
    }
}
