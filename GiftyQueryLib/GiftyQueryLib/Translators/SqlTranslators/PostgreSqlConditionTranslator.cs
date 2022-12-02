using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Utils;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using C = GiftyQueryLib.Config.QueryConfig;

namespace GiftyQueryLib.Translators.SqlTranslators
{
    /// <summary>
    /// PostgreSQL condition translator that translates Expression tree into the base SQL "where" syntax<br/>
    /// </summary>
    public class PostgreSqlConditionTranslator : BaseConditionTranslator
    {
        public PostgreSqlConditionTranslator() : base()
        {
            expressionTypes = new()
            {
                { ExpressionType.Convert, new [] { string.Empty } },
                { ExpressionType.Not, new [] { " NOT " } },
                { ExpressionType.And, new [] { " AND " } },
                { ExpressionType.AndAlso, new [] { " AND " } },
                { ExpressionType.Or, new [] { " OR " } },
                { ExpressionType.OrElse, new [] { " OR " } },
                { ExpressionType.Equal, new[] { " IS ", " = " } },
                { ExpressionType.NotEqual, new[] { " IS NOT ", " != " } },
                { ExpressionType.LessThan, new[] { " < " } },
                { ExpressionType.LessThanOrEqual, new[] { " <= " } },
                { ExpressionType.GreaterThan, new[] { " > " } },
                { ExpressionType.GreaterThanOrEqual, new[] { " >= " } },
                { ExpressionType.Add, new[] { " + " } },
                { ExpressionType.Subtract, new[] { " - " } },
                { ExpressionType.Multiply, new[] { " * " } },
                { ExpressionType.Divide, new[] { " / " } },
                { ExpressionType.Modulo, new[] { " % " } },
                { ExpressionType.ExclusiveOr, new[] { " ^ " } }
            };

            aggregateFunctions = new Dictionary<string, string>
            {
                { "PCount", "COUNT({0})" },
                { "PSum", "SUM({0})" },
                { "PAvg", "AVG({0})" },
                { "PMin", "MIN({0})" },
                { "PMax", "MAX({0})" },
                { "PConcat", "CONCAT({0})" }
            };
        }

        /// <summary>
        /// Parse Anonymus Selector into string format
        /// </summary>
        /// <param name="anonymusSelector">The selector of anonymus type</param>
        /// <param name="exceptSelector">The selector to exclude params. Only works if anonymus selector has "All" claim</param>
        /// <param name="extraType">Extra Type</param>
        /// <returns>Member Data Collection</returns>
        /// <exception cref="ArgumentException"></exception>
        public override SelectorData ParseAnonymousSelector<TItem>(Expression<Func<TItem, object>>? anonymusSelector, Expression<Func<TItem, object>>? exceptSelector = null, Type? extraType = null) where TItem : class
        {
            var body = anonymusSelector?.Body;
            var isSelectAll = false;

            if (body is null)
                throw new ArgumentException($"The anonymus selector is null");

            if (body is NewExpression newExpression && newExpression.Type.Name.Contains("Anonymous"))
            {
                type = typeof(TItem);

                int i = 0;

                var sb = new StringBuilder();

                foreach (var exp in newExpression.Arguments)
                {
                    string? paramName = newExpression?.Members?[i]?.Name?.ToString()?.ToCaseFormat();

                    if (exp is MemberExpression memberExp)
                        sb.Append(ParseMemberExpression(memberExp, paramName));
                    else if (exp is MethodCallExpression callExp)
                        sb.Append(ParseMethodCallExpression(callExp, paramName));
                    else if (exp is BinaryExpression bExp)
                        sb.Append(ParseBinaryExpression(bExp, paramName));
                    else if (exp is ConstantExpression cExp)
                    {
                        if (cExp.Value?.ToString() == SelectType.All.ToString())
                        {
                            isSelectAll = true;
                            sb.Append(ParsePropertiesOfType(exceptSelector, extraType));
                        }
                    }

                    i++;
                }

                string result = sb.ToString();
                if (result.EndsWith(','))
                    result = result.Remove(result.Length - 1, 1);

                return new SelectorData { Result = result, ExtraData = new { IsSelectAll = isSelectAll } };
            }

            throw new ArgumentException($"{nameof(ParseAnonymousSelector)} - Invalid expression was provided");
        }

        /// <summary>
        /// Get all property data of type
        /// </summary>
        /// <typeparam name="TItem">Type of entity</typeparam>
        /// <param name="exceptSelector">Data that should not be included into selection</param>
        /// <param name="extraType">Extra type</param>
        protected virtual string ParsePropertiesOfType<TItem>(Expression<Func<TItem, object>>? exceptSelector = null, Type? extraType = null)
        {
            var exceptMembers = new List<MemberInfo>();
            var sb = new StringBuilder();

            if (exceptSelector is not null)
            {
                var body = exceptSelector?.Body;
                if (body is not null && body is NewExpression newExpression && newExpression.Type.Name.Contains("Anonymous"))
                {
                    foreach (var exp in newExpression.Arguments)
                    {
                        if (exp is MemberExpression memberExp)
                            exceptMembers.Add(memberExp.Member);
                    }
                }
            }

            var type = extraType ?? typeof(TItem);
            foreach (var property in type.GetProperties())
            {
                var attributeData = property.GetCustomAttributesData();

                CustomAttributeData? notMappedAttrData = null;
                CustomAttributeData? foreignKeyAttrData = null;

                foreach (var attr in attributeData)
                {
                    if (C.NotMappedAttributes.Any(type => attr.AttributeType == type)) notMappedAttrData = attr;
                    if (C.ForeignKeyAttributes.Any(type => attr.AttributeType == type)) foreignKeyAttrData = attr;
                }

                if (notMappedAttrData is not null || (exceptSelector is not null && exceptMembers.Any(it => it.Name == property.Name)))
                    continue;

                sb.Append(foreignKeyAttrData is not null
                    ? string.Format(C.ColumnAccessFormat + ",", C.Scheme, type.ToCaseFormat(), foreignKeyAttrData.ConstructorArguments[0]!.Value!.ToString()!.ToCaseFormat())
                    : string.Format(C.ColumnAccessFormat + ",", C.Scheme, type.ToCaseFormat(), property.Name.ToCaseFormat()));
            }

            var parsed = sb.ToString();
            return parsed;
        }

        /// <summary>
        /// Parse Member Expression into string
        /// </summary>
        /// <param name="memberExp"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        protected virtual string ParseMemberExpression(MemberExpression memberExp, string? paramName = null)
        {
            var sb = new StringBuilder();

            if (!C.NotMappedAttributes.Any(attr => memberExp.Member.GetCustomAttribute(attr) is not null))
            {
                var memberAttributes = GetMemberAttributeArguments(memberExp.Member);
                var memberName = memberAttributes is null
                    ? memberExp.Member.Name.ToCaseFormat()
                    : memberAttributes.FirstOrDefault().ToString();

                if (memberName == paramName)
                    sb.AppendFormat(C.ColumnAccessFormat + ",", C.Scheme, memberExp.Expression?.Type.ToCaseFormat(), memberName);
                else
                    sb.AppendFormat(C.ColumnAccessFormat + " AS {3},", C.Scheme, memberExp.Expression?.Type.ToCaseFormat(), memberName, paramName);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parse Method Call Expression into string
        /// </summary>
        /// <param name="callExp"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected virtual string ParseMethodCallExpression(MethodCallExpression callExp, string? paramName = null)
        {
            var sb = new StringBuilder();

            var methodName = callExp.Method.Name;
            var arguments = callExp.Arguments;

            string? translatedInnerExpression = null;

            Type? type = null;
            MemberInfo? memberInfo = null;

            if (arguments.Count == 2)
            {
                if (arguments[1] is UnaryExpression uExp)
                {
                    if (uExp is not null && uExp.Operand is BinaryExpression bExp)
                    {
                        var translator = new PostgreSqlConditionTranslator();
                        translatedInnerExpression = translator.Translate(base.type!, bExp);
                    }
                    else
                        throw new ArgumentException($"The parameter of function/method is invalid in anonymous expression");
                }
                else if (arguments[1] is MemberExpression mExp)
                {
                    type = mExp.Expression?.Type;
                    memberInfo = mExp.Member;
                }
                else if (arguments[1] is BinaryExpression bExp)
                {
                    var translator = new PostgreSqlConditionTranslator();
                    translatedInnerExpression = translator.Translate(base.type!, bExp);
                }
                else if (arguments[1] is NewArrayExpression nExp)
                {
                    if (CheckIfMethodExists(methodName, aggregateFunctions))
                    {
                        if (methodName == "PConcat")
                        {
                            if (nExp.Expressions.Count < 2)
                            {
                                throw new ArgumentException($"Cannot use CONCAT function with 1 or less number of arguments");
                            }

                            var columns = nExp.Expressions.Select(it =>
                            {
                                if (it is MemberExpression mExp)
                                {
                                    var fkArgument = GetMemberAttributeArguments(mExp?.Member)?.FirstOrDefault();

                                    var memeberName = fkArgument?.Value is not null ? fkArgument.Value.ToString() : mExp?.Member.Name.ToCaseFormat();
                                    return string.Format(C.ColumnAccessFormat, C.Scheme, mExp?.Expression?.Type.ToCaseFormat(), memeberName);
                                }
                                else if (it is UnaryExpression uExp)
                                {
                                    if (uExp is not null)
                                    {
                                        var operand = uExp.Operand;
                                        var translator = new PostgreSqlConditionTranslator();
                                        return translator.Translate(base.type!, operand);
                                    }
                                    else
                                        throw new ArgumentException($"The parameter of function/method is invalid in anonymous expression");
                                }
                                else
                                    throw new ArgumentException($"The parameter of function/method is invalid in anonymous expression");

                            });

                            return string.Format(aggregateFunctions[methodName], string.Join(',', columns)) + (paramName is null ? "" : " AS " + paramName);
                        } 
                    }
                }
            }

            if (arguments[0] is UnaryExpression uArg)
            {
                if (uArg.Operand is not MemberExpression operand)
                    throw new ArgumentException($"The operand of the unary expression is null");

                type = operand.Expression?.Type;
                memberInfo = operand.Member;
            }
            else if (arguments[0] is MemberExpression mArg)
            {
                type = mArg.Expression?.Type;
                memberInfo = mArg.Member;
            }
            else if (arguments[0] is ParameterExpression pArg)
            {
                type = pArg.Type;
            }
            else
                throw new ArgumentException($"Unsupported expression in provided arguments");


            var memberAttributes = GetMemberAttributeArguments(memberInfo);

            if (translatedInnerExpression is null)
            {
                if (memberInfo is null)
                    throw new ArgumentException($"Invalid method call on provided expression");

                string format = C.ColumnAccessFormat;

                if (CheckIfMethodExists(methodName, aggregateFunctions))
                {
                    format = string.Format(aggregateFunctions[methodName], format) + (paramName is null ? "" : " AS " + paramName);
                }

                var memberName = memberAttributes is null
                    ? memberInfo.Name.ToCaseFormat()
                    : memberAttributes.FirstOrDefault().ToString();

                sb.AppendFormat(format + ",", C.Scheme, type?.ToCaseFormat(), memberName);
            }
            else
            {
                if (CheckIfMethodExists(methodName, aggregateFunctions))
                {
                    string format = aggregateFunctions[methodName] + (paramName is null ? "" : " AS " + paramName);
                    sb.AppendFormat(format + ",", translatedInnerExpression);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parse Binary Expression into string
        /// </summary>
        /// <param name="bExp"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        protected virtual string ParseBinaryExpression(BinaryExpression bExp, string? paramName = null)
        {
            var sb = new StringBuilder();

            if (Constants.TypesToStringCast.Contains(bExp.Type))
                throw new ArgumentException($"Binary expression cannot be parsed when left or right operands have type {bExp.Type}. If you want concat strings use PConcat function instead.");

            var translator = new PostgreSqlConditionTranslator();
            string translatedBinary = translator.Translate(type!, bExp);

            sb.AppendFormat(paramName is null ? "{0}," : "{0} AS {1},", translatedBinary, paramName);

            return sb.ToString();
        }

        /// <summary>
        /// Checks if method exists
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="functions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected virtual bool CheckIfMethodExists(string? methodName, Dictionary<string, string> functions) =>
            methodName is not null && functions.ContainsKey(methodName)
            ? true
            : throw new ArgumentException($"Funtion '{methodName}' is not registered into dictionary {nameof(aggregateFunctions)}");


        protected override Expression VisitUnary(UnaryExpression u)
        {
            try
            {
                string value = expressionTypes[u.NodeType][0];
                if (!string.IsNullOrEmpty(value))
                    sb.Append(value);
                Visit(u.Operand);
                return u;
            }
            catch
            {
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            try
            {
                if (!expressionTypes.ContainsKey(b.NodeType))
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

                sb.Append('(');
                Visit(b.Left);

                string[] values = expressionTypes[b.NodeType];
                sb.Append(values.Length > 1 ? values[IsNullConstant(b.Right) ? 0 : 1] : values[0]);

                Visit(b.Right);
                sb.Append(')');
                return b;
            }
            catch
            {
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable? q = c.Value as IQueryable;
            sb.Append(q is null && c.Value is null ? "NULL" : c.Value is string st ? string.Format("'{0}'", st) : c.Value);
            return c;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression is not null && (m.Expression.NodeType == ExpressionType.Parameter || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                sb.Append(string.Format(C.ColumnAccessFormat, C.Scheme, type?.ToCaseFormat(), m.Member.Name.ToCaseFormat()));
                return m;
            }

            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (aggregateFunctions.ContainsKey(m.Method.Name))
            {
                string parsed = ParseMethodCallExpression(m);

                if (parsed.EndsWith(','))
                    parsed = parsed.Remove(parsed.Length - 1, 1);

                sb.Append(parsed);
                return m;
            }

            sb.Append(GetMethodTranslated(m));

            return m;
        }

        protected virtual string GetMethodTranslated(MethodCallExpression m)
        {
            return m.Method.Name switch
            {
                "Contains" => GetContainsMethodTranslated(m),
                "ToLower" or "ToLowerInvariant" => GetUpperLowerMethodTranslated(m, true),
                "ToUpper" or "ToUpperInvariant" => GetUpperLowerMethodTranslated(m, false),
                "ToString" => GetToStringMethodTranslated(m),
                _ => string.Empty,
            };
        }

        protected virtual string GetContainsMethodTranslated(MethodCallExpression m)
        {
            var sb = new StringBuilder();
            string arg = string.Empty;
            Expression? expObj = null;

            if (m.Arguments.Count == 1)
            {
                if (m.Arguments[0] is ConstantExpression cArg)
                {
                    var mObj = m.Object as MemberExpression;

                    sb.Append(string.Format(" " + C.ColumnAccessFormat + " LIKE '%{3}%' ",
                        C.Scheme,
                        mObj!.Expression!.Type.ToCaseFormat(),
                        mObj.Member.Name.ToCaseFormat(),
                        cArg.Value));

                    return sb.ToString();
                }
                else if (m.Arguments[0] is MemberExpression mArg)
                {
                    arg = mArg.Member.Name;
                    expObj = m.Object;
                }
                else
                    throw new ArgumentException("Method argument is invalid");

            }
            else if (m.Arguments.Count == 2)
            {
                arg = ((MemberExpression)m.Arguments[1]).Member.Name;
                expObj = m.Arguments[0];
            }

            if (expObj is null)
                throw new ArgumentException("Object should not be null");

            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(expObj.Type);
            bool isArray = typeof(Array).IsAssignableFrom(expObj.Type);

            if (expObj is NewArrayExpression or ListInitExpression)
            {
                void localAction(Expression exp, List<object> items)
                {
                    if (exp is ConstantExpression cExp)
                        items.Add(ConvertToItemWithType(cExp.Value));
                    else if (exp is MethodCallExpression mExp)
                        items.Add(GetMethodTranslated(mExp));
                    else
                        throw new ArgumentException("Invalid data");
                }

                var items = new List<object>();

                if (expObj is NewArrayExpression arrExp)
                {
                    var exps = arrExp.Expressions;

                    foreach (var e in exps)
                        localAction(e, items);

                }
                else if (expObj is ListInitExpression listExp)
                {
                    var inits = listExp.Initializers;

                    if (!inits.Any())
                        items.Add(string.Empty);

                    foreach (var init in inits)
                        localAction(init.Arguments[0], items);
                }

                sb.Append(string.Format(" " + C.ColumnAccessFormat + " IN ({3}) ", C.Scheme, type?.ToCaseFormat(), arg.ToCaseFormat(), string.Join(',', items)));
                return sb.ToString();
            }

            var obj = (expObj as ConstantExpression)!;

            if ((obj.Type.IsGenericType && obj.Type.GenericTypeArguments.Length == 1 && isEnumerable) || isArray)
            {
                var genericArg = isArray ? obj.Type : obj.Type.GenericTypeArguments[0];

                object? val = obj.Value;

                if (val is null)
                    throw new ArgumentException("Object value should not be null");

                bool result = false;
                result = PerformTypeBasedEnum<string>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<char>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<Guid>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<DateTime>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<TimeSpan>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<int>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<short>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<ushort>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<uint>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<float>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<double>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<long>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<ulong>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<byte>(genericArg, val, arg, result, sb);
                result = PerformTypeBasedEnum<decimal>(genericArg, val, arg, result, sb);

                if (!result)
                    throw new ArgumentException($"Unsupported type {genericArg?.Name}");

                return sb.ToString();
            }
            else
            {
                throw new ArgumentException($"Unsupported type {obj.Type}");
            }
        }

        protected virtual string GetUpperLowerMethodTranslated(MethodCallExpression m, bool isLower)
        {
            if (m.Object is null)
                throw new ArgumentException("Object should not be null");

            if (m.Object is MemberExpression memberExp)
            {
                return string.Format(" " + (isLower ? "LOWER" : "UPPER") + "(" + C.ColumnAccessFormat + ")", C.Scheme, type?.ToCaseFormat(), memberExp.Member.Name.ToCaseFormat());
            }
            else if (m.Object is ConstantExpression constExp)
            {
                string? value = constExp.Value?.ToString();
                value = isLower ? value?.ToLower() : value?.ToUpper();
                return string.Format("'{0}' ", value);
            }

            return string.Empty;
        }

        protected virtual string GetToStringMethodTranslated(MethodCallExpression m)
        {
            if (m.Object is null)
                throw new ArgumentException("Object should not be null");

            if (m.Object is MemberExpression memberExp)
            {
                return string.Format(C.ColumnAccessFormat + "::varchar(255)", C.Scheme, type?.ToCaseFormat(), memberExp.Member.Name.ToCaseFormat());
            }
            else if (m.Object is ConstantExpression constExp)
            {
                return string.Format("'{0}' ", constExp?.Value?.ToString());
            }

            return string.Empty;
        }

        protected void AppendInStatement<TItem>(IEnumerable<TItem> items, string arg, StringBuilder sb)
        {
            if (items is not null && items.Any())
            {
                if (Constants.TypesToStringCast.Contains(typeof(TItem)))
                    sb.Append(string.Format(" " + C.ColumnAccessFormat + " IN ({3}) ", C.Scheme, type?.ToCaseFormat(), arg.ToCaseFormat(), string.Join(',', items.Select(it => $"'{it}'"))));
                else
                    sb.Append(string.Format(" " + C.ColumnAccessFormat + " IN ({3}) ", C.Scheme, type?.ToCaseFormat(), arg.ToCaseFormat(), string.Join(',', items)));
            }
        }

        protected static string ConvertToItemWithType(object? item)
        {
            if (item is null)
                return string.Empty;

            if (Constants.TypesToStringCast.Contains(item.GetType()))
                return string.Format("'{0}'", item.ToString());
            else
                return string.Format("{0}", item.ToString());
        }

        protected bool PerformTypeBasedEnum<TItem>(Type genericType, object valueToCast, string expressionType, bool result, StringBuilder sb)
        {
            if (result)
                return true;

            if (genericType == typeof(TItem) || genericType == typeof(TItem[]))
            {
                AppendInStatement((IEnumerable<TItem>)valueToCast, expressionType, sb);
                return true;
            }

            return false;
        }
    }
}
