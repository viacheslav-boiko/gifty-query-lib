﻿using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Functions;
using GiftyQueryLib.Queries.PostgreSQL;
using System.Linq.Expressions;

namespace GiftyQueryLib.Builders.PostgreSql
{
    public interface IPostgreSqlBuilder
    {
        #region Customized Configuration
        /// <summary>
        /// Set up custom configuration for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config">PostgreSQL Configuration</param>
        /// <returns></returns>
        public IInstructionNode<T> UseConfig<T>(PostgreSqlConfig config) where T : class;

        /// <summary>
        /// Set up custom scheme for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheme">PostgreSQL Scheme</param>
        /// <returns></returns>
        public IInstructionNode<T> UseScheme<T>(string scheme) where T : class;

        /// <summary>
        /// Set up custom case for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="caseType">PostgreSQL Case Type</param>
        /// <param name="caseFormatterFunc">PostgreSQL Case Formatter Func</param>
        /// <returns></returns>
        public IInstructionNode<T> UseCaseFormat<T>(CaseType caseType, Func<string, string>? caseFormatterFunc = null) where T : class;

        #endregion

        #region Basic Queries

        /// <summary>
        /// PostgreSQL Select Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rowSelector">Row to count by</param>
        /// <returns></returns>
        IJoinNode<T> Count<T>(Expression<Func<T, object>>? rowSelector = null) where T : class;

        /// <summary>
        /// PostgreSQL Select Distinct Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rowSelector">Row to count by</param>
        /// <returns></returns>
        IJoinNode<T> CountDistinct<T>(Expression<Func<T, object>>? rowSelector = null) where T : class;

        /// <summary>
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public IJoinNode<T> Select<T>(Expression<Func<T, object>> include) where T : class;

        /// <summary>
        /// PostgreSQL Select Distinct Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public IJoinNode<T> SelectDistinct<T>(Expression<Func<T, object>> include) where T : class;

        /// <summary>
        /// PostgreSQL Select All Entity Properties Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <returns></returns>
        public IJoinNode<T> SelectAll<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null) where T : class;

        /// <summary>
        /// PostgreSQL Select Distinct All Entity Properties Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <returns></returns>
        public IJoinNode<T> SelectDistinctAll<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null) where T : class;

        /// <summary>
        /// PostgreSQL Insert Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to insert</param>
        public IQueryStringBuilder Insert<T>(params T[] entities) where T : class;

        /// <summary>
        /// PostgreSQL Update Single Entity Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entity to update</param>
        public IEditConditionNode<T> Update<T>(object entity) where T : class;

        /// <summary>
        /// PostgreSQL Update Multiple Entities Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to update</param>
        public IQueryStringBuilder UpdateRange<T>(params object[] entities) where T : class;

        /// <summary>
        /// PostgreSQL Delete Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public IEditConditionNode<T> Delete<T>() where T : class;

        #endregion

        #region Functions

        /// <summary>
        /// Transforms into <b>COUNT(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public int Count(object _);

        /// <summary>
        /// Transforms into <b>MIN(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Min(object _);

        /// <summary>
        /// Transforms into <b>MAX(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Max(object _);

        /// <summary>
        /// Transforms into <b>AVG(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Avg(object _);

        /// <summary>
        /// Transforms into <b>SUM(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Sum(object _);

        /// <summary>
        /// Transforms into <b>CONCAT(str1, str2, ...)</b> sql concatenation function
        /// </summary>
        /// <returns></returns>
        public string Concat(params object?[] _);

        /// <summary>
        /// Gets alias value by it's name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T Alias<T>(string name);

        /// <summary>
        /// Gets autogenerated alias for single-count queries
        /// </summary>
        /// <returns></returns>
        public double Alias();

        /// <summary>
        /// Transforms into <b>DISTINCT ON (coumn_name) column_name_alias</b> sql expression
        /// </summary>
        /// <returns></returns>
        public string Distinct(object _);

        #endregion

        /// <summary>
        /// PostgreSQL Functions
        /// </summary>
        public PostgreSqlFunctions Func { get; }
    }

    public class PostgreSqlBuilder : IPostgreSqlBuilder
    {
        #region Configuration

        /// <summary>
        /// PostgreSQL Configuration
        /// </summary>
        public PostgreSqlConfig Config { get; init; } = new();

        /// <summary>
        /// PostgreSQL Functions
        /// </summary>
        public PostgreSqlFunctions Func => PostgreSqlFunctions.Instance;

        #endregion


        #region Queries

        /// <summary>
        /// Set up custom configuration for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config">PostgreSQL Configuration</param>
        /// <returns></returns>
        public IInstructionNode<T> UseConfig<T>(PostgreSqlConfig config) where T : class
        {
            return PostgreSqlQuery<T>.Flow(config, Func);
        }

        /// <summary>
        /// Set up custom scheme for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheme">PostgreSQL Scheme</param>
        /// <returns></returns>
        public IInstructionNode<T> UseScheme<T>(string scheme) where T : class
        {
            var config = new PostgreSqlConfig(Config) { Scheme = scheme };
            return PostgreSqlQuery<T>.Flow(config, Func);
        }

        /// <summary>
        /// Set up custom case for PostgreSQL builder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="caseType">PostgreSQL Case Type</param>
        /// <param name="caseFormatterFunc">PostgreSQL Case Formatter Func</param>
        /// <returns></returns>
        public IInstructionNode<T> UseCaseFormat<T>(CaseType caseType, Func<string, string>? caseFormatterFunc = null) where T : class
        {
            var config = new PostgreSqlConfig(Config) { CaseType = caseType };
            if (caseFormatterFunc is not null && caseType == CaseType.Custom)
                config.CaseFormatterFunc = caseFormatterFunc;

            return PostgreSqlQuery<T>.Flow(config, Func);
        }

        /// <summary>
        /// PostgreSQL Select Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rowSelector">Row to count by</param>
        /// <returns></returns>
        public IJoinNode<T> Count<T>(Expression<Func<T, object>>? rowSelector = null) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).Count(rowSelector);
        }

        /// <summary>
        /// PostgreSQL Select Distinct Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rowSelector">Row to count by</param>
        /// <returns></returns>
        public IJoinNode<T> CountDistinct<T>(Expression<Func<T, object>>? rowSelector = null) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).CountDistinct(rowSelector);
        }

        /// <summary>
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public virtual IJoinNode<T> Select<T>(Expression<Func<T, object>> include) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).Select(include);
        }

        /// <summary>
        /// PostgreSQL Select Distinct Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public IJoinNode<T> SelectDistinct<T>(Expression<Func<T, object>> include) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).SelectDistinct(include);
        }

        /// <summary>
        /// PostgreSQL Select All Entity Properties Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <returns></returns>
        public IJoinNode<T> SelectAll<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).SelectAll(include, exclude);
        }

        /// <summary>
        /// PostgreSQL Select Distinct All Entity Properties Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <returns></returns>
        public IJoinNode<T> SelectDistinctAll<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).SelectDistinctAll(include, exclude);
        }

        /// <summary>
        /// PostgreSQL Insert Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to insert</param>
        public virtual IQueryStringBuilder Insert<T>(params T[] entities) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).Insert(entities);
        }

        /// <summary>
        /// PostgreSQL Update Single Entity Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entity to update</param>
        public virtual IEditConditionNode<T> Update<T>(object entity) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).Update(entity);
        }

        /// <summary>
        /// PostgreSQL Update Multiple Entities Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to update</param>
        public IQueryStringBuilder UpdateRange<T>(params object[] entities) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).UpdateRange(entities);
        }

        /// <summary>
        /// PostgreSQL Delete Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public virtual IEditConditionNode<T> Delete<T>() where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config, Func).Delete();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Transforms into <b>COUNT(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public int Count(object _) => default;

        /// <summary>
        /// Transforms into <b>MIN(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Min(object _) => default;

        /// <summary>
        /// Transforms into <b>MAX(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Max(object _) => default;

        /// <summary>
        /// Transforms into <b>AVG(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Avg(object _) => default;

        /// <summary>
        /// Transforms into <b>SUM(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Sum(object _) => default;

        /// <summary>
        /// Transforms into <b>CONCAT(str1, str2, ...)</b> sql concatenation function
        /// </summary>
        /// <returns></returns>
        public string Concat(params object?[] _) => string.Empty;

        /// <summary>
        /// Gets alias value by it's name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T Alias<T>(string name) => default!;

        /// <summary>
        /// Gets autogenerated alias for single-count queries
        /// </summary>
        /// <returns></returns>
        public double Alias() => default;

        /// <summary>
        /// Transforms into <b>DISTINCT ON (coumn_name) column_name_alias</b> sql expression
        /// </summary>
        /// <returns></returns>
        public string Distinct(object _) => default!;


        #endregion
    }

    #region Enums

    public enum JoinType
    {
        Inner, Left, Right, Full, Cross
    }

    #endregion
}
