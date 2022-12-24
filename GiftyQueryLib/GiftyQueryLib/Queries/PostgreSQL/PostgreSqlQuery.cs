using GiftyQueryLib.Builders.PostgreSql;
using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using System.Linq.Expressions;

namespace GiftyQueryLib.Queries.PostgreSQL
{
    public class PostgreSqlQuery<T> :
       IInstructionNode<T>, IEditConditionNode<T>, IJoinNode<T>, IWhereNode<T>, IGroupNode<T>, IHavingNode<T>, IOrderNode<T>, ILimitNode, IOffsetNode where T : class
    {
        private readonly PostgreSqlQueryHelper<T> helper;

        public PostgreSqlQuery(PostgreSqlConfig config)
        {
            helper = new(config);
        }

        public virtual IJoinNode<T> Count(Expression<Func<T, object>>? columnSelector = null)
        {
            helper.Count(columnSelector);
            return this;
        }

        public IJoinNode<T> CountDistinct(Expression<Func<T, object>>? columnSelector = null)
        {
            helper.Count(columnSelector, distinct: true);
            return this;
        }

        public virtual IJoinNode<T> Select(Expression<Func<T, object>> include)
        {
            helper.Select(include);
            return this;
        }

        public IJoinNode<T> SelectDistinct(Expression<Func<T, object>> include)
        {
            helper.Select(include, distinct: true);
            return this;
        }

        public IJoinNode<T> SelectSingle(Expression<Func<T, object>>? columnSelector = null)
        {
            helper.SelectSingle(columnSelector);
            return this;
        }

        public IJoinNode<T> SelectDistinctSingle(Expression<Func<T, object>>? columnSelector = null)
        {
            helper.SelectSingle(columnSelector, distinct: true);
            return this;
        }

        public IJoinNode<T> SelectAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null)
        {
            helper.Select(include, exclude, selectAll: true);
            return this;
        }

        public IJoinNode<T> SelectDistinctAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null)
        {
            helper.Select(include, exclude, selectAll: true, distinct: true);
            return this;
        }

        public virtual IQueryStringBuilder Insert(params T[] entities)
        {
            helper.Insert(entities);
            return this;
        }

        public virtual IEditConditionNode<T> Update(object entity)
        {
            helper.Update(entity);
            return this;
        }

        public IQueryStringBuilder UpdateRange(params object[] entities)
        {
            helper.UpdateRange(entities);
            return this;
        }

        public virtual IEditConditionNode<T> Delete()
        {
            helper.Delete();
            return this;
        }

        public virtual IJoinNode<T> Join(Expression<Func<T, object>> selector, JoinType joinType = JoinType.Inner)
        {
            helper.Join(selector, joinType);
            return this;
        }

        public virtual IJoinNode<T> Join<U>(Expression<Func<U, object>> selector, JoinType joinType = JoinType.Inner)
        {
            helper.Join(selector, joinType);
            return this;
        }

        public virtual IWhereNode<T> Where(Expression<Func<T, bool>> condition)
        {
            helper.Where(condition);
            return this;
        }

        public virtual IQueryStringBuilder Predicate(Expression<Func<T, bool>> condition)
        {
            helper.Where(condition);
            return this;
        }

        public virtual IOffsetNode Limit(int limit)
        {
            helper.Limit(limit);
            return this;
        }

        public virtual IQueryStringBuilder Offset(int skipped)
        {
            helper.Offset(skipped);
            return this;
        }

        public virtual IHavingNode<T> Group(Expression<Func<T, object>> include, Expression<Func<T, object>>? exclude = null)
        {
            helper.Group(include, exclude);
            return this;
        }

        public virtual IOrderNode<T> Having(Expression<Func<T, bool>> condition)
        {
            helper.Having(condition);
            return this;
        }

        public virtual IOrderNode<T> Order(Expression<Func<T, object>> columnSelector, OrderType orderType = OrderType.Asc)
        {
            helper.Order(columnSelector, orderType);
            return this;
        }

        public virtual string Build()
        {
            return helper.ToString();
        }
    }
}
