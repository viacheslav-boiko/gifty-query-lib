﻿using GiftyQueryLib.Enums;
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

    public interface ILimitNode : IQueryStringBuilder, IOffsetNode
    {
        IOffsetNode Limit(int limit);
    }

    public interface IOrderNode<T> : IQueryStringBuilder, ILimitNode where T : class
    {
        ILimitNode Order(params (Expression<Func<T, object>> rowSelector, OrderType orderType)[] rowsForOrdering);
    }

    public interface IHavingNode<T> : IQueryStringBuilder, IOrderNode<T> where T : class
    {
        IOrderNode<T> Having(Expression<Func<T, bool>> condition);
    }

    public interface IGroupNode<T> : IQueryStringBuilder, IHavingNode<T> where T : class
    {
        IHavingNode<T> Group(Expression<Func<T, object>> groupingObject);

        IHavingNode<T> Group<U>(Expression<Func<U, object>> groupingObject) where U : class;
    }

    public interface IWhereNode<T> : IQueryStringBuilder, IGroupNode<T> where T : class
    {
        IWhereNode<T> Where(Expression<Func<T, bool>> condition);
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
