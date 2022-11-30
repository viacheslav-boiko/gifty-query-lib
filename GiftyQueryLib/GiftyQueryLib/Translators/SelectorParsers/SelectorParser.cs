using GiftyQueryLib.Translators.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace GiftyQueryLib.Translators.SelectorParsers
{
    /// <summary>
    /// A class to parse selectors
    /// </summary>
    public static class SelectorParser
    {
        /// <summary>
        /// Parse Property Selector into simplified member data item
        /// </summary>
        /// <param name="rowSelector">Property Row Selector</param>
        /// <returns>Member Data</returns>
        /// <exception cref="ArgumentException"></exception>
        public static MemberData GetMemberData<T>(Expression<Func<T, object>> propertySelector)
        {
            var body = propertySelector?.Body;

            if (body is null)
                throw new ArgumentException($"{GetMemberData<T>} - The row selector is null");

            if (body is MemberExpression memberExpression)
            {
                return new MemberData
                {
                    MemberType = body.Type,
                    CallerType = memberExpression.Expression!.Type,
                    MemberInfo = memberExpression.Member
                };
            }
            else if (body is UnaryExpression unaryExpression)
            {
                return new MemberData
                {
                    MemberType = unaryExpression.Type,
                    CallerType = null,
                    MemberInfo = GetMemberName(unaryExpression)
                };
            }
            else
                throw new ArgumentException($"{nameof(GetMemberData)} - Invalid expression was provided");
        }

        private static MemberInfo GetMemberName(UnaryExpression unaryExpression)
        {
            return unaryExpression.Operand is MethodCallExpression methodExpression
                    ? methodExpression.Method
                    : ((MemberExpression)unaryExpression.Operand).Member;
        }
    }
}
