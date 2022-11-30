using System.Linq.Expressions;
using System.Reflection;

namespace GiftyQueryLib.Translators
{
    internal sealed class DynamicConstantVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            var expr = Visit(node.Expression);
            if (expr is ConstantExpression c)
            {
                if (node.Member is PropertyInfo prop)
                    return Expression.Constant(prop.GetValue(c.Value), prop.PropertyType);
                if (node.Member is FieldInfo field)
                    return Expression.Constant(field.GetValue(c.Value), field.FieldType);
            }
            return node.Update(expr);
        }
    }
}
