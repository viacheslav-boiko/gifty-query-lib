using System.Linq.Expressions;
using System.Text;

namespace GiftyQueryLib.Translators
{
    /// <summary>
    /// Base translator that translates Expression tree into other language statements
    /// </summary>
    public abstract class BaseTranslator : ExpressionVisitor
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
        protected BaseTranslator()
        {
            sb = new StringBuilder();
            type = null;
        }

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
        public virtual string Translate(Type type, Expression expression)
        {
            this.type = type;
            Visit(new DynamicConstantVisitor().Visit(expression));
            string result = sb.ToString();
            sb = new StringBuilder();
            return result;
        }
    }
}
