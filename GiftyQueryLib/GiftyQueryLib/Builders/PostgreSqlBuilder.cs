using GiftyQueryLib.Config;
using GiftyQueryLib.Queries.PostgreSQL;
using System.Linq.Expressions;

namespace GiftyQueryLib.Builders
{
    public interface IPostgreSqlBuilder
    {
        /// <summary>
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <param name="distinct">Is selection distinct</param>
        public IConditionNode<T> Select<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false) where T : class;

        /// <summary>
        /// PostgreSQL Insert Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to insert</param>
        public IQueryStringBuilder Insert<T>(params T[] entities) where T : class;

        /// <summary>
        /// PostgreSQL Update Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to update</param>
        public IEditConditionNode<T> Update<T>(params T[] entities) where T : class;

        /// <summary>
        /// PostgreSQL Delete Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public IEditConditionNode<T> Delete<T>() where T : class;
    }

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
        /// PostgreSQL Select Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="include">Columns to include into selection</param>
        /// <param name="exclude">Columns to exclude from selection</param>
        /// <param name="distinct">Is selection distinct</param>
        public virtual IConditionNode<T> Select<T>(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config).Select(include, exclude, distinct);
        }

        /// <summary>
        /// PostgreSQL Insert Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to insert</param>
        public virtual IQueryStringBuilder Insert<T>(params T[] entities) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config).Insert(entities);
        }

        /// <summary>
        /// PostgreSQL Update Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities">Entities to update</param>
        public virtual IEditConditionNode<T> Update<T>(params T[] entities) where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config).Update(entities);
        }

        /// <summary>
        /// PostgreSQL Delete Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public virtual IEditConditionNode<T> Delete<T>() where T : class
        {
            return PostgreSqlQuery<T>.Flow(Config).Delete();
        }

        #endregion
    }
}
