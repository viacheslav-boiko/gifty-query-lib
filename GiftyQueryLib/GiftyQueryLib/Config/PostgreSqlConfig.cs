﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GiftyQueryLib.Config
{
    /// <summary>
    /// Class to configure settings of PostgreSQL query builder
    /// </summary>
    public class PostgreSqlConfig
    {
        public PostgreSqlConfig() { }

        public PostgreSqlConfig(PostgreSqlConfig config)
        {
            CaseConfig = config.CaseConfig;
            UseNamesProvidedInColumnAttribute = config.UseNamesProvidedInColumnAttribute;
            UseNamesProvidedInTableAttribute = config.UseNamesProvidedInTableAttribute;
            Scheme = config.Scheme;
            ColumnAccessFormat = config.ColumnAccessFormat;
        }

        /// <summary>
        /// Case Formatter Configuration
        /// </summary>
        public CaseConfig CaseConfig { get; init; } = new();

        /// <summary>
        /// If flag is <b>true</b>, system will take database column name that set in Column attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database column name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public bool UseNamesProvidedInColumnAttribute { get; set; } = false;

        /// <summary>
        /// If flag is <b>true</b>, system will take database table name that set in Table attribute while creating the query<br/>
        /// If flag is <b>false</b>, system will use specified case type to convert it to database table name while creating the query
        /// Default: <b>false</b>
        /// </summary>
        public bool UseNamesProvidedInTableAttribute { get; set; } = false;

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
        /// List of attribute types that mark field as a json field<br/>
        /// Default: <b>JsonIncludeAttribute</b>
        /// </summary>
        public HashSet<Type> JsonAttributes { get; } = new HashSet<Type>
        {
            typeof(JsonIncludeAttribute)
        };

        /// <summary>
        /// Scheme name for databases that support schemes infrastructure
        /// Default: <b>public</b>
        /// </summary>
        public string Scheme { get; set; } = "public";

        /// <summary>
        /// Column access for query builder
        /// Default: <b>{0}.{1}.{2}</b> (0 - scheme, 1 - table, 2 - column)
        /// </summary>
        public string ColumnAccessFormat { get; set; } = "{0}.{1}.{2}";
    }
}
