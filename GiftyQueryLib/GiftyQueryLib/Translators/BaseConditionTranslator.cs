using GiftyQueryLib.Exceptions;
using GiftyQueryLib.Translators.Models;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GiftyQueryLib.Translators
{
    /// <summary>
    /// Base condition translator that translates Expression tree into other language statements
    /// </summary>
    public abstract class BaseConditionTranslator : ExpressionVisitor
    {
        /// <summary>
        /// Input type
        /// </summary>
        protected Type? type;

        /// <summary>
        /// Result translated string
        /// </summary>
        protected StringBuilder sb;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="caseType">Naming format</param>
        protected BaseConditionTranslator()
        {
            sb = new StringBuilder();
            type = null;
        }

        /// <summary>
        /// Parse Anonymus Selector into string format
        /// </summary>
        /// <param name="anonymusSelector">The selector of anonymus type</param>
        /// <param name="exceptSelector">The selector to exclude params. Only works if anonymus selector has "All" claim</param>
        /// <param name="extraType">Extra Type</param>
        /// <param name="config">Selector Config</param>
        /// <returns>Selector Data</returns>
        public abstract SelectorData ParseAnonymousSelector<TItem>(
            Expression<Func<TItem, object>>? anonymusSelector,
            Expression<Func<TItem, object>>? exceptSelector = null,
            Type? extraType = null, SelectorConfig? config = null) where TItem : class;

        /// <summary>
        /// Converts an Expression statement into other language statement string
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual string Translate<TItem>(Expression expression) where TItem : class
        {
            type = typeof(TItem);
            return Translate(type, expression);
        }

        /// <summary>
        /// Parse Property Selector into simplified member data item
        /// </summary>
        /// <param name="propertySelector">Property Row Selector</param>
        /// <returns>Member Data</returns>
        /// <exception cref="ArgumentException"></exception>
        public virtual MemberData GetMemberData<TItem>(Expression<Func<TItem, object>> propertySelector)
        {
            var body = propertySelector?.Body;

            if (body is null)
                throw new BuilderException("The row selector is null");

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
                MemberInfo? memberInfo;

                if (unaryExpression.Operand is MethodCallExpression mcExp)
                    memberInfo = mcExp.Method;
                else if (unaryExpression.Operand is MemberExpression mExp)
                    memberInfo = mExp.Member;
                else
                    throw new BuilderException("Invalid operand expression");

                return new MemberData
                {
                    MemberType = unaryExpression.Type,
                    CallerType = unaryExpression.Operand is MethodCallExpression ? null : ((MemberExpression)unaryExpression.Operand)?.Expression?.Type,
                    MemberInfo = memberInfo
                };
            }
            else
                throw new BuilderException($"Invalid expression was provided");
        }

        /// <summary>
        /// Converts an Expression statement into other language statement string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected virtual string Translate(Type type, Expression expression)
        {
            this.type = type;
            Visit(new DynamicConstantVisitor().Visit(expression));
            string result = sb.ToString();
            sb = new StringBuilder();
            return result;
        }

        /// <summary>
        /// Checks if expression is a null constant
        /// </summary>
        /// <param name="exp">Expression</param>
        /// <returns></returns>
        protected static bool IsNullConstant(Expression exp)
        {
            return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
        }

        /// <summary>
        /// Gets Member's Atribute Arguments <br/>
        /// <b>Default:</b> Foreign Key Attributes as HashSet
        /// </summary>
        /// <param name="memberInfo">Member Info</param>
        /// <param name="attributeTypes">Attribute Types to retrieve arguments of them</param>
        /// <returns></returns>
        protected IList<CustomAttributeTypedArgument>? GetMemberAttributeArguments(MemberInfo? memberInfo, HashSet<Type> attributes, HashSet<Type>? attributeTypes = null)
        {
            if (memberInfo is null)
                return null;

            var foreignKeyAttr = memberInfo.CustomAttributes
                        .FirstOrDefault(attr => (attributeTypes is null ? attributes : attributeTypes).Any(type => attr.AttributeType == type));

            if (foreignKeyAttr is null)
                return null;

            return foreignKeyAttr.ConstructorArguments;
        }
    }
}
