using GiftyQueryLib.Builders.PostgreSql;
using GiftyQueryLib.Enums;
using System.Linq.Expressions;

namespace GiftyQueryLib.Queries.PostgreSQL
{
    public interface IQueryStringBuilder
    {
        string Build();
    }

    public interface IOffsetNode : IQueryStringBuilder
    {
        IQueryStringBuilder Offset(int skipped);
    }

    public interface ILimitNode : IOffsetNode
    {
        IOffsetNode Limit(int limit);
    }

    public interface IOrderNode<T> : ILimitNode where T : class
    {
        IOrderNode<T> Order(Expression<Func<T, object>> columnSelector, OrderType orderType = OrderType.Asc);
    }

    public interface IHavingNode<T> : IOrderNode<T> where T : class
    {
        IOrderNode<T> Having(Expression<Func<T, bool>> condition);
    }

    public interface IGroupNode<T> : IHavingNode<T> where T : class
    {
        IHavingNode<T> Group(Expression<Func<T, object>> include, Expression<Func<T, object>>? exclude = null);
    }

    public interface IWhereNode<T> : IGroupNode<T> where T : class
    {
        IWhereNode<T> Where(Expression<Func<T, bool>> condition);
    }

    public interface IJoinNode<T> : IWhereNode<T> where T : class
    {
        IJoinNode<T> Join(Expression<Func<T, object>> selector, JoinType joinType = JoinType.Inner);

        IJoinNode<T> Join<U>(Expression<Func<U, object>> selector, JoinType joinType = JoinType.Inner);
    }

    public interface IEditConditionNode<T> : IQueryStringBuilder where T : class
    {
        IQueryStringBuilder Predicate(Expression<Func<T, bool>> condition);
    }

    public interface IInstructionNode<T> : IQueryStringBuilder where T : class
    {
        IJoinNode<T> Count(Expression<Func<T, object>>? columnSelector = null);

        IJoinNode<T> CountDistinct(Expression<Func<T, object>>? columnSelector = null);

        IJoinNode<T> Select(Expression<Func<T, object>> include);

        IJoinNode<T> SelectSingle(Expression<Func<T, object>>? columnSelector = null);

        IJoinNode<T> SelectDistinctSingle(Expression<Func<T, object>>? columnSelector = null);

        IJoinNode<T> SelectDistinct(Expression<Func<T, object>> include);

        IJoinNode<T> SelectAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null);

        IJoinNode<T> SelectDistinctAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null);

        IQueryStringBuilder Insert(params T[] entities);

        IEditConditionNode<T> Update(object entity);

        IQueryStringBuilder UpdateRange(params object[] entities);

        IEditConditionNode<T> Delete();
    }
}
