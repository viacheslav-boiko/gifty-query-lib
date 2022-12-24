using GiftyQueryLib.Config;
using GiftyQueryLib.Exceptions;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Utils;
using System.Linq.Expressions;
using System.Text;

namespace GiftyQueryLib.Translators.SqlTranslators
{
    /// <summary>
    /// PostgreSQL translator that translates Expression tree into the base SQL statements
    /// </summary>
    public class PostgreSqlTranslator : BaseTranslator
    {
        private readonly PostgreSqlConfig config;

        /// <summary>
        /// PostgreSQL Translator Helper contains helper methods for Expression parsing
        /// </summary>
        public PostgreSqlTranslatorHelper Helper { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sqlConfig"></param>
        public PostgreSqlTranslator(PostgreSqlConfig sqlConfig) : base()
        {
            config = sqlConfig;
            Helper ??= new PostgreSqlTranslatorHelper(config);
        }

        #region Selector

        /// <summary>
        /// Parse Selector into string format
        /// </summary>
        /// <param name="anonymusSelector">The selector to include items</param>
        /// <param name="exceptSelector">The selector to exclude items</param>
        /// <param name="extraType">Extra Type</param>
        /// <param name="config">Selector Config</param>
        /// <returns>Parsed data</returns>
        /// <exception cref="BuilderException"></exception>
        public virtual string ParseSelector<TItem>(Expression<Func<TItem, object>>? anonymusSelector, Expression<Func<TItem, object>>? exceptSelector = null,
                                                   Type? extraType = null, SelectorConfig? config = null) where TItem : class
        {
            var body = anonymusSelector?.Body;

            config ??= new SelectorConfig();

            if (body is null && !config.SelectAll)
                throw new BuilderException("The anonymus selector cannot be null");

            var sb = new StringBuilder();

            if (config.SelectAll)
                sb.Append(Helper.ParsePropertiesOfType(exceptSelector, extraType));

            if (body is NewExpression newExpression && newExpression.Type.Name.Contains("Anonymous"))
            {
                type = typeof(TItem);

                int i = 0;

                foreach (var exp in newExpression.Arguments)
                {
                    var paramName = config.UseAliases ? newExpression?.Members?[i]?.Name?.ToString()?.ToCaseFormat(this.config.CaseConfig) : null;
                    var exception = "Anonymous selector has not allowed expressions that could be parsed";

                    if (exp is MemberExpression memberExp)
                    {
                        if (!config.AllowMember)
                            throw new BuilderException(exception);
                        sb.Append(Helper.ParseMemberExpression(memberExp, paramName));
                    }

                    else if (exp is MethodCallExpression callExp)
                    {
                        if (!config.AllowMethodCall)
                            throw new BuilderException(exception);
                        sb.Append(Helper.ParseMethodCallExpression(callExp, type, paramName, true));
                    }

                    else if (exp is BinaryExpression bExp)
                    {
                        if (!config.AllowBinary)
                            throw new BuilderException(exception);
                        sb.Append(Helper.ParseBinaryExpression(bExp, type, paramName));
                    }
                    else if (exp is ConstantExpression cExp)
                    {
                        if (!config.AllowConstant)
                            throw new BuilderException(exception);
                        sb.Append(Helper.ParseConstantExpession(cExp, paramName));
                    }

                    i++;
                }
            }

            return sb.TrimEndComma();
        }

        #endregion

        #region Unary

        /// <summary>
        /// Visits unary expression
        /// </summary>
        /// <param name="u">Unary expression</param>
        /// <returns>Expression</returns>
        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Not && u.Operand is not null and MemberExpression mExp && (mExp.Type == typeof(bool) || mExp.Type == typeof(bool?)))
            {
                var type = mExp.Expression?.Type.ToCaseFormat(config.CaseConfig);
                var name = mExp.Member.Name.ToCaseFormat(config.CaseConfig);
                sb.AppendFormat(config.ColumnAccessFormat + " = FALSE ", config.Scheme, type, name);
                return u;
            }

            var value = Helper.ExpressionTypes[u.NodeType][0];
            if (!string.IsNullOrEmpty(value))
                sb.Append(value);
            Visit(u.Operand);
            return u;
        }

        #endregion

        #region Binary

        /// <summary>
        /// Visits binary expression
        /// </summary>
        /// <param name="b">Binary expression</param>
        /// <returns>Expression</returns>
        /// <exception cref="BuilderException"></exception>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (!Helper.ExpressionTypes.ContainsKey(b.NodeType))
                throw new BuilderException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

            sb.Append('(');
            Visit(b.Left);

            var values = Helper.ExpressionTypes[b.NodeType];
            var isNull = b.Right.NodeType == ExpressionType.Constant && ((ConstantExpression)b.Right).Value == null;

            sb.Append(values.Length > 1 ? values[isNull ? 0 : 1] : values[0]);

            Visit(b.Right);
            sb.Append(')');
            return b;
        }

        #endregion

        #region Constant

        /// <summary>
        /// Visits constant expression
        /// </summary>
        /// <param name="c">Constant expression</param>
        /// <returns>Expression</returns>
        protected override Expression VisitConstant(ConstantExpression c)
        {
            sb.Append(Helper.ParseConstantExpession(c));
            return c;
        }

        #endregion

        #region Member

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression is not null && (m.Expression.NodeType == ExpressionType.Parameter || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                if (m.Member.Name == "Length" && m.Expression is MemberExpression mExp && mExp.Type == typeof(string))
                {
                    var targetType = mExp.Expression?.Type?.ToCaseFormat(config.CaseConfig);
                    sb.AppendFormat($"LENGTH({config.ColumnAccessFormat})", config.Scheme, targetType, mExp.Member.Name.ToCaseFormat(config.CaseConfig));
                }
                else
                {
                    var targetType = m.Expression.Type;
                    sb.AppendFormat(config.ColumnAccessFormat, config.Scheme, targetType?.ToCaseFormat(config.CaseConfig), m.Member.Name.ToCaseFormat(config.CaseConfig));
                }

                return m;
            }
            else if (m.NodeType == ExpressionType.MemberAccess)
            {
                if (m.Type == typeof(DateTime))
                {
                    var dateTimeProp = typeof(DateTime).GetProperty(m.Member.Name);
                    if (dateTimeProp is not null)
                    {
                        var value = (DateTime?)dateTimeProp.GetValue(null);
                        sb.Append(value is null ? "NULL" : $"'{value}'");
                    }
                }

                return m;
            }

            throw new BuilderException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        #endregion

        #region Method Call

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Type == typeof(DateTime))
            {
                var obj = m.Object;

                if (obj is not null and MemberExpression mExp)
                {
                    var dateTimeProp = typeof(DateTime).GetProperty(mExp.Member.Name);
                    if (dateTimeProp is not null)
                    {
                        var value = (DateTime?)dateTimeProp.GetValue(null);
                        var result = m.Method.Invoke(value, null);
                        sb.Append($"'{(result is null ? "NULL" : result)}'");
                    }
                }
                return m;
            }

            var mathTypes = new[] { "Math", "IPostgreSqlMathFunctions" };
            if (mathTypes.Contains(m.Method.DeclaringType?.Name))
            {
                var parsed = Helper.ParseMathMethodCallExpression(m, type!);
                sb.Append(parsed);
                return m;
            }

            if (Helper.Functions.ContainsKey(m.Method.Name))
            {
                var parsed = Helper.ParseMethodCallExpression(m, type!).TrimEndComma();
                sb.Append(parsed);
                return m;
            }

            sb.Append(Helper.GetMethodTranslated(m, type!));

            return m;
        }

        #endregion
    }
}
