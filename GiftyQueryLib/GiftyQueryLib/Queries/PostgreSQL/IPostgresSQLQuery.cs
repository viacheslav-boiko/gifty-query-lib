using GiftyQueryLib.Enums;
using System.Linq.Expressions;

namespace GiftyQueryLib.Queries.PostgreSQL
{
    public interface IQueryStringBuilder
    {
        string Build();
    }

    public interface ISelectNode : IQueryStringBuilder
    {
    }

    public interface IOffsetNode : ISelectNode
    {
        IQueryStringBuilder Offset(int skipped);
    }

    public interface ILimitNode : ISelectNode, IOffsetNode
    {
        IOffsetNode Limit(int limit);
    }

    public interface IOrderNode<T> : ISelectNode, ILimitNode where T : class
    {
        IOrderNode<T> Order(Expression<Func<T, object>> rowSelector, OrderType orderType = OrderType.Asc);
    }

    public interface IHavingNode<T> : ISelectNode, IOrderNode<T> where T : class
    {
        IOrderNode<T> Having(Expression<Func<T, bool>> condition);
    }

    public interface IGroupNode<T> : ISelectNode, IHavingNode<T> where T : class
    {
        IHavingNode<T> Group(Expression<Func<T, object>> include, Expression<Func<T, object>>? exclude = null);
    }

    public interface IWhereNode<T> : ISelectNode, IGroupNode<T> where T : class
    {
        IWhereNode<T> Where(Expression<Func<T, bool>> condition);
    }

    public interface IJoinNode<T> : ISelectNode, IWhereNode<T> where T : class
    {
        IJoinNode<T> Join(Expression<Func<T, object>> selector, JoinType joinType = JoinType.Inner);

        IJoinNode<T> Join<U>(Expression<Func<U, object>> selector, JoinType joinType = JoinType.Inner);
    }

    public interface IConditionNode<T> : ISelectNode, IJoinNode<T> where T : class
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

        IEditConditionNode<T> Update(object entity);

        IQueryStringBuilder UpdateRange(params object[] entities);

        IEditConditionNode<T> Delete();
    }
}
