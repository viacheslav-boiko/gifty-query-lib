using GiftyQueryLib.Enums;
using System.Linq.Expressions;

namespace GiftyQueryLib.Queries.QueryNodes
{
    public interface IQueryStringBuilder
    {
        string Build();
    }

    public interface ILimitNode : IQueryStringBuilder
    {
        IQueryStringBuilder Offset(int skipped);
    }

    public interface IOrderNode<T> : IQueryStringBuilder, ILimitNode where T : class
    {
        ILimitNode Limit(int limit);
    }

    public interface IKeySetPaginationNode<T> : IQueryStringBuilder, IOrderNode<T> where T : class
    {
        IOrderNode<T> Order(params (Expression<Func<T, object>> rowSelector, OrderType orderType)[] rowsForOrdering);
    }

    public interface IGroupNode<T> : IKeySetPaginationNode<T>, IQueryStringBuilder where T : class
    {
    }

    public interface IWhereNode<T> : IQueryStringBuilder, IKeySetPaginationNode<T> where T : class
    {
        IWhereNode<T> Where(Expression<Func<T, bool>> condition);
        
        // TODO: Keyset pagination
    }

    public interface IJoinNode<T> : IQueryStringBuilder, IWhereNode<T> where T : class
    {
        IJoinNode<T> Join(Expression<Func<T, object>> selector, JoinType joinType = JoinType.Inner);

        IJoinNode<T> Join<U>(Expression<Func<U, object>> selector, JoinType joinType = JoinType.Inner);
    }

    public interface IConditionNode<T> : IQueryStringBuilder, IJoinNode<T> where T : class
    {
        IJoinNode<T> Count(Expression<Func<T, object>>? rowSelector = null, CountType countType = CountType.Count);
    }

    public interface IEditConditionNode<T> : IQueryStringBuilder where T : class
    {
        IQueryStringBuilder Predicate(Expression<Func<T, bool>> condition);
    }

    public interface IInstructionNode<T> : IQueryStringBuilder where T : class
    {
        IConditionNode<T> Select(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false);

        IQueryStringBuilder Insert(params T[] entities);

        IEditConditionNode<T> Update(params T[] entities);

        IEditConditionNode<T> Delete();
    }
}
