using GiftyQueryLib.Enums;
using GiftyQueryLib.Queries.QueryNodes;
using System.Linq.Expressions;

namespace GiftyQueryLib.Queries
{
    //public abstract class Query<T> : IInstructionNode<T>, IWhereNode<T>, IOrderNode<T>,
    //    IKeySetPaginationNode<T>, ILimitNode, IEditConditionNode<T>,
    //    IGroupNode<T> where T : class
    //{
    //    public abstract string Build();
    //    public abstract IJoinNode<T> Count(Expression<Func<T, object>>? rowSelector = null, CountType countType = CountType.Count);
    //    public abstract IEditConditionNode<T> Delete();
    //    public abstract IQueryStringBuilder Insert(params T[] entities);
    //    public abstract ILimitNode Limit(int limit);
    //    public abstract IQueryStringBuilder Offset(int skipped);
    //    public abstract IOrderNode<T> Order(params (Expression<Func<T, object>> rowSelector, OrderType orderType)[] rowsForOrdering);
    //    public abstract IQueryStringBuilder Predicate(Expression<Func<T, bool>> condition);
    //    public abstract IConditionNode<T> Select(Expression<Func<T, object>>? rowSelector = null, bool distinct = false);
    //    public abstract IConditionNode<T> Except(Expression<Func<T, object>>? rowSelector);
    //    public abstract IEditConditionNode<T> Update(params T[] entities);
    //    public abstract IWhereNode<T> Where(Expression<Func<T, bool>> condition);
    //}
}
