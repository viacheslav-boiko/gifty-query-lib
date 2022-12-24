using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Queries.PostgreSQL;
using System.Linq.Expressions;

namespace GiftyQueryLib.Builders.PostgreSql
{
    public class PostgreSqlBuilder : IPostgreSqlBuilder
    {
        #region Configuration

        /// <summary>
        /// PostgreSQL Configuration
        /// </summary>
        public PostgreSqlConfig Config { get; init; } = new();

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
            return new PostgreSqlQuery<T>(config);
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
            return new PostgreSqlQuery<T>(config);
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
            var config = new PostgreSqlConfig(Config);
            config.CaseConfig.CaseType = caseType;
            if (caseFormatterFunc is not null)
                config.CaseConfig.CaseFormatterFunc = caseFormatterFunc;

            return new PostgreSqlQuery<T>(config);
        }

        /// <summary>
        /// PostgreSQL Select Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnSelector">Column to count by</param>
        /// <returns></returns>
        public IJoinNode<T> Count<T>(Expression<Func<T, object>>? columnSelector = null) where T : class
        {
            return new PostgreSqlQuery<T>(Config).Count(columnSelector);
        }

        /// <summary>
        /// PostgreSQL Select Distinct Count Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnSelector">Column to count by</param>
        /// <returns></returns>
        public IJoinNode<T> CountDistinct<T>(Expression<Func<T, object>>? columnSelector = null) where T : class
        {
            return new PostgreSqlQuery<T>(Config).CountDistinct(columnSelector);
        }

        /// <summary>
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public virtual IJoinNode<T> Select<T>(Expression<Func<T, object>> include) where T : class
        {
            return new PostgreSqlQuery<T>(Config).Select(include);
        }

        /// <summary>
        /// PostgreSQL Select Single Column Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnSelector">Column to select</param>
        /// <returns></returns>
        public IJoinNode<T> SelectSingle<T>(Expression<Func<T, object>>? columnSelector = null) where T : class
        {
            return new PostgreSqlQuery<T>(Config).SelectSingle(columnSelector);
        }

        /// <summary>
        /// PostgreSQL Select Distinct Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        public IJoinNode<T> SelectDistinct<T>(Expression<Func<T, object>> include) where T : class
        {
            return new PostgreSqlQuery<T>(Config).SelectDistinct(include);
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
            return new PostgreSqlQuery<T>(Config).SelectAll(include, exclude);
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
            return new PostgreSqlQuery<T>(Config).SelectDistinctAll(include, exclude);
        }

        /// <summary>
        /// PostgreSQL Insert Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to insert</param>
        public virtual IQueryStringBuilder Insert<T>(params T[] entities) where T : class
        {
            return new PostgreSqlQuery<T>(Config).Insert(entities);
        }

        /// <summary>
        /// PostgreSQL Update Single Entity Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entity to update</param>
        public virtual IEditConditionNode<T> Update<T>(object entity) where T : class
        {
            return new PostgreSqlQuery<T>(Config).Update(entity);
        }

        /// <summary>
        /// PostgreSQL Update Multiple Entities Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to update</param>
        public IQueryStringBuilder UpdateRange<T>(params object[] entities) where T : class
        {
            return new PostgreSqlQuery<T>(Config).UpdateRange(entities);
        }

        /// <summary>
        /// PostgreSQL Delete Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public virtual IEditConditionNode<T> Delete<T>() where T : class
        {
            return new PostgreSqlQuery<T>(Config).Delete();
        }

        #endregion
    }

    #region Enums

    public enum JoinType
    {
        Inner, Left, Right, Full, Cross
    }

    #endregion
}
