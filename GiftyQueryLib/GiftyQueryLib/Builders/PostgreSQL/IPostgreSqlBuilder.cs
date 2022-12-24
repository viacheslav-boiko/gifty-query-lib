using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Functions.PostgreSQL;
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
        /// <param name="columnSelector">Column to count by</param>
        /// <returns></returns>
        public IJoinNode<T> Count<T>(Expression<Func<T, object>>? columnSelector = null) where T : class;

        /// <summary>
        /// PostgreSQL Select Distinct Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnSelector">Column to count by</param>
        /// <returns></returns>
        public IJoinNode<T> CountDistinct<T>(Expression<Func<T, object>>? columnSelector = null) where T : class;

        /// <summary>
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public IJoinNode<T> Select<T>(Expression<Func<T, object>> include) where T : class;

        /// <summary>
        /// PostgreSQL Select Single Column Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnSelector">Column to select</param>
        /// <returns></returns>
        public IJoinNode<T> SelectSingle<T>(Expression<Func<T, object>>? columnSelector = null) where T : class;

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
        public string Concat(params object?[] _) => default!;

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

        /// <summary>
        /// Transforms into <b>EXISTS(expression)</b> sql subquery
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool Exists<T>(Expression<Func<T, bool>> predicate) => default;

        /// <summary>
        /// Represents PostgreSQL-specific Math functions
        /// </summary>
        public IPostgreSqlMathFunctions Math => default!;

        #endregion

        #region Subqueries

        /// <summary>
        /// Transforms into <b>EXISTS(expression)</b> sql subquery<br/>
        /// Allows to create custom select subquery
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectQuery"></param>
        /// <returns></returns>
        public bool Exists(ISelectQuery selectQuery) => default;

        #endregion
    }
}
