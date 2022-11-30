using GiftyQueryLib.Config;
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
        /// Possible expression types and them analogs
        /// </summary>
        protected Dictionary<ExpressionType, string[]> expressionTypes;

        /// <summary>
        /// Aggregate functions and them analogs
        /// </summary>
        protected Dictionary<string, string> aggregateFunctions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="caseType">Naming format</param>
        protected BaseConditionTranslator()
        {
            sb = new StringBuilder();

            type = null;
            expressionTypes = new();
            aggregateFunctions = new();
        }

        /// <summary>
        /// Parse Anonymus Selector into string format
        /// </summary>
        /// <param name="anonymusSelector">The selector of anonymus type</param>
        /// <param name="exceptSelector">The selector to exclude params. Only works if anonymus selector has "All" claim</param>
        /// <param name="extraType">Extra type</param>
        /// <returns>Member Data Collection</returns>
        /// <exception cref="ArgumentException"></exception>
        public abstract SelectorData ParseAnonymousSelector<TItem>(Expression<Func<TItem, object>>? anonymusSelector, Expression<Func<TItem, object>>? exceptSelector = null, Type? extraType = null) where TItem : class;

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
        ///  Gets Operand From Expression
        /// </summary>
        /// <param name="exp">Expression</param>
        /// <returns></returns>
        public string GetOperandFromExpression<TInput>(Expression<Func<TInput, object>>? exp) where TInput : class
        {
            if (exp is not null && exp.Body is UnaryExpression uExp && uExp.NodeType == ExpressionType.Convert)
            {
                return (uExp.Operand as ConstantExpression)!.Value!.ToString()!;
            }

            return string.Empty;
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
        protected IList<CustomAttributeTypedArgument>? GetMemberAttributeArguments(MemberInfo? memberInfo, HashSet<Type>? attributeTypes = null)
        {
            if (memberInfo is null)
                return null;

            var foreignKeyAttr = memberInfo.CustomAttributes
                        .FirstOrDefault(attr => (attributeTypes is null ? GiftyQueryConfig.ForeignKeyAttributes : attributeTypes).Any(type => attr.AttributeType == type));

            if (foreignKeyAttr is null)
                return null;

            return foreignKeyAttr.ConstructorArguments;
        }
    }
}
