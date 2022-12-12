using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Exceptions;
using GiftyQueryLib.Functions;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Utils;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GiftyQueryLib.Translators.SqlTranslators
{
    /// <summary>
    /// PostgreSQL condition translator that translates Expression tree into the base SQL "where" syntax and performs selectors parsing
    /// </summary>
    public class PostgreSqlConditionTranslator : BaseConditionTranslator
    {
        private readonly PostgreSqlConfig config;
        private readonly CaseFormatterConfig caseConfig;
        private readonly PostgreSqlFunctions func;
        private readonly Dictionary<string, string> aliases;

        public PostgreSqlConditionTranslator(PostgreSqlConfig config, PostgreSqlFunctions func) : base()
        {
            this.aliases = new();
            this.config = config;
            this.caseConfig = new CaseFormatterConfig { CaseType = config.CaseType, CaseFormatterFunc = config.CaseFormatterFunc };
            this.func = func;
        }

        /// <summary>
        /// Returns attribute data for property
        /// </summary>
        /// <param name="property">Target property</param>
        /// <returns></returns>
        public virtual (CustomAttributeData? keyAttrData, CustomAttributeData? notMappedAttrData, CustomAttributeData? foreignKeyAttrData) GetAttrData(PropertyInfo? property)
        {
            var attributeData = property is null ? new List<CustomAttributeData>() : property.GetCustomAttributesData();

            CustomAttributeData? keyAttrData = null;
            CustomAttributeData? notMappedAttrData = null;
            CustomAttributeData? foreignKeyAttrData = null;

            foreach (var attr in attributeData)
            {
                if (config.KeyAttributes.Any(type => attr.AttributeType == type)) keyAttrData = attr;
                if (config.NotMappedAttributes.Any(type => attr.AttributeType == type)) notMappedAttrData = attr;
                if (config.ForeignKeyAttributes.Any(type => attr.AttributeType == type)) foreignKeyAttrData = attr;
            }

            return (keyAttrData, notMappedAttrData, foreignKeyAttrData);
        }

        /// <summary>
        /// Returns properties and their values from entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">Entity to parse</param>
        /// <param name="removeKeyProp">Determine if remove key prop or not</param>
        /// <returns></returns>
        /// <exception cref="BuilderException"></exception>
        public virtual Dictionary<string, object?> GetPopertyWithValueOfEntity<T>(T entity, bool removeKeyProp = true) where T : class
        {
            if (entity is null)
                throw new BuilderException("Entity cannot be null");

            var dict = new Dictionary<string, object?>();
            var props = entity.GetType().GetProperties();

            foreach (var property in props)
            {
                var (keyAttrData, notMappedAttrData, foreignKeyAttrData) = GetAttrData(property);

                if ((keyAttrData is not null && removeKeyProp) || notMappedAttrData is not null)
                    continue;

                string propName = string.Empty;
                object? value = null;

                if (foreignKeyAttrData is not null)
                {
                    if (!property.IsCollection())
                    {
                        propName = foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig);

                        var keyProp = property.PropertyType.GetProperties().FirstOrDefault(prop => prop.GetCustomAttributes(true)
                              .FirstOrDefault(attr => config.KeyAttributes.Any(it => attr.GetType() == it)) is not null);
                        if (keyProp is null)
                            throw new BuilderException($"Related table class {property.Name} does not contain key attribute");

                        var keyPropValue = property.GetValue(entity);
                        value = keyProp.GetValue(keyPropValue);
                    }
                    else
                    {
                        var genericArg = property.GetGenericArg();
                        if (Constants.StringTypes.Contains(genericArg!) || Constants.NumericTypes.Contains(genericArg!))
                            throw new BuilderException("Primitive-typed collection should not be marked with foreign key attributes");
                        else
                        {
                            // Handle many to many
                            continue;
                        }
                    }
                }
                else
                {
                    if (!property.IsCollection())
                    {
                        propName = property.Name.ToCaseFormat(caseConfig);
                        value = property.GetValue(entity);
                    }
                    else
                    {
                        var genericArg = property.GetGenericArg();
                        if (Constants.StringTypes.Contains(genericArg!) || Constants.NumericTypes.Contains(genericArg))
                        {
                            propName = property.Name.ToCaseFormat(caseConfig);
                            // TODO Handle PostgreSQL Arrays
                        }
                        else
                            continue;
                    }
                }

                dict.Add(propName, value);
            }

            return dict;
        }

        /// <summary>
        /// Parse Anonymus Selector into string format
        /// </summary>
        /// <param name="anonymusSelector">The selector of anonymus type</param>
        /// <param name="exceptSelector">The selector to exclude params. Only works if anonymus selector has "All" claim</param>
        /// <param name="extraType">Extra Type</param>
        /// <param name="useAliases">Determine if use aliases for columns or not</param>
        /// <returns>Member Data Collection</returns>
        /// <exception cref="BuilderException"></exception>
        public override SelectorData ParseAnonymousSelector<TItem>(
            Expression<Func<TItem, object>>? anonymusSelector,
            Expression<Func<TItem, object>>? exceptSelector = null,
            Type? extraType = null,
            bool allowMemberExpression = true,
            bool allowMethodCallExpression = true,
            bool allowBinaryExpression = true,
            bool allowConstantExpression = true,
            bool useAliases = true) where TItem : class
        {
            var body = anonymusSelector?.Body;
            var isSelectAll = false;

            if (body is null)
                throw new BuilderException($"The anonymus selector is null");

            if (body is NewExpression newExpression && newExpression.Type.Name.Contains("Anonymous"))
            {
                type = typeof(TItem);

                int i = 0;

                var sb = new StringBuilder();

                foreach (var exp in newExpression.Arguments)
                {
                    string? paramName = useAliases ? newExpression?.Members?[i]?.Name?.ToString()?.ToCaseFormat(caseConfig) : null;

                    if (exp is MemberExpression memberExp)
                    {
                        if (!allowMemberExpression)
                            throw new BuilderException("Anonymous selector has not allowed expressions that cannot be parsed");
                        sb.Append(ParseMemberExpression(memberExp, paramName));
                    }

                    else if (exp is MethodCallExpression callExp)
                    {
                        if (!allowMethodCallExpression)
                            throw new BuilderException("Anonymous selector has not allowed expressions that cannot be parsed");
                        sb.Append(ParseMethodCallExpression(callExp, paramName, true));
                    }

                    else if (exp is BinaryExpression bExp)
                    {
                        if (!allowBinaryExpression)
                            throw new BuilderException("Anonymous selector has not allowed expressions that cannot be parsed");
                        sb.Append(ParseBinaryExpression(bExp, paramName));
                    }
                    else if (exp is ConstantExpression cExp)
                    {
                        if (!allowConstantExpression)
                            throw new BuilderException("Anonymous selector has not allowed expressions that cannot be parsed");

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

            throw new BuilderException($"Invalid expression was provided");
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
                var (keyAttrData, notMappedAttrData, foreignKeyAttrData) = GetAttrData(property);

                if (notMappedAttrData is not null || (exceptSelector is not null && exceptMembers.Any(it => it.Name == property.Name)))
                    continue;

                sb.Append(foreignKeyAttrData is not null
                    ? string.Format(config.ColumnAccessFormat + ",", config.Scheme, type.ToCaseFormat(caseConfig), foreignKeyAttrData.ConstructorArguments[0]!.Value!.ToString()!.ToCaseFormat(caseConfig))
                    : string.Format(config.ColumnAccessFormat + ",", config.Scheme, type.ToCaseFormat(caseConfig), property.Name.ToCaseFormat(caseConfig)));
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
            if (!config.NotMappedAttributes.Any(attr => memberExp.Member.GetCustomAttribute(attr) is not null))
            {
                var memberAttributes = GetMemberAttributeArguments(memberExp.Member, config.ForeignKeyAttributes);
                var memberName = memberAttributes is null
                    ? memberExp.Member.Name.ToCaseFormat(caseConfig)
                    : memberAttributes.FirstOrDefault().ToString().Replace("\"", "");

                var result = string.Format(config.ColumnAccessFormat, config.Scheme, memberExp.Expression?.Type.ToCaseFormat(caseConfig), memberName);

                if (paramName is null || memberExp.Member.Name.ToCaseFormat(caseConfig) == paramName)
                    return result + ",";
                else
                {
                    if (!aliases.TryAdd(paramName, result))
                        throw new BuilderException($"Alias \"{paramName}\" already exists");

                    return string.Format(result + " AS {0},", paramName);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Parse Method Call Expression into string
        /// </summary>
        /// <param name="callExp"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        /// <exception cref="BuilderException"></exception>
        protected virtual string ParseMethodCallExpression(MethodCallExpression callExp, string? paramName = null, bool isSelectorParsing = false)
        {
            var sb = new StringBuilder();

            var methodName = callExp.Method.Name;
            var arguments = callExp.Arguments;

            string? translatedInnerExpression = null;

            Type? type = null;
            MemberInfo? memberInfo = null;

            if (arguments.Count == 2)
            {
                if (arguments[1] is MemberExpression mExp)
                {
                    type = mExp.Expression?.Type;
                    memberInfo = mExp.Member;
                }
                else if (arguments[1] is BinaryExpression bExp)
                {
                    var translator = new PostgreSqlConditionTranslator(config, func);
                    translatedInnerExpression = translator.Translate(base.type!, bExp);
                }
                else
                    throw new BuilderException($"The parameter of function/method is invalid in anonymous expression");
            }

            if (arguments[0] is UnaryExpression uArg)
            {
                if (uArg is not null && uArg.Operand is MemberExpression operand)
                {
                    type = operand.Expression?.Type;
                    memberInfo = operand.Member;
                }
                else if (uArg is not null && uArg.Operand is BinaryExpression bExp)
                {
                    var translator = new PostgreSqlConditionTranslator(config, func);
                    translatedInnerExpression = translator.Translate(base.type!, bExp);
                }
                else
                    throw new BuilderException($"The parameter of function/method is invalid in anonymous expression");

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
            else if (arguments[0] is NewArrayExpression nExp)
            {
                if (CheckIfMethodExists(methodName, func.Functions!))
                {
                    if (methodName == "Concat")
                    {
                        if (nExp.Expressions.Count < 2)
                            throw new BuilderException($"Cannot use CONCAT function with 1 or less number of arguments");

                        var columns = nExp.Expressions.Select(it =>
                        {
                            if (it is MemberExpression mExp)
                            {
                                var fkArgument = GetMemberAttributeArguments(mExp?.Member, config.ForeignKeyAttributes)?.FirstOrDefault();

                                var memeberName = fkArgument?.Value is not null ? fkArgument.Value.ToString() : mExp?.Member.Name.ToCaseFormat(caseConfig);
                                return string.Format(config.ColumnAccessFormat, config.Scheme, mExp?.Expression?.Type.ToCaseFormat(caseConfig), memeberName);
                            }
                            else if (it is UnaryExpression uExp && uExp is not null)
                            {
                                var operand = uExp.Operand;
                                var translator = new PostgreSqlConditionTranslator(config, func);
                                return translator.Translate(base.type!, operand);
                            }
                            else if (it is MethodCallExpression mcExp && mcExp is not null)
                            {
                                var translator = new PostgreSqlConditionTranslator(config, func);
                                return translator.Translate(base.type!, mcExp);
                            }
                            else
                                throw new BuilderException($"The parameter of function/method is invalid in anonymous expression");

                        });

                        var result = string.Format(func.Functions[methodName].value, string.Join(',', columns));

                        if (paramName is null)
                            return result + ",";

                        if (!aliases.TryAdd(paramName, result))
                            throw new BuilderException($"Alias \"{paramName}\" already exists");

                        return result + (paramName is null ? "" : " AS " + paramName) + ",";
                    }
                }
            }
            else if (arguments[0] is ConstantExpression cEx)
            {
                if (methodName == "Alias")
                {
                    var alias = cEx?.Value?.ToString()?.ToCaseFormat(caseConfig);
                    if (alias is null)
                        throw new BuilderException($"Alias cannot be null or empty while using HAVING statement");

                    var aliasExists = aliases.TryGetValue(alias, out string? value);

                    if (!aliasExists)
                        throw new BuilderException($"Alias \"{cEx?.Value}\" does not exist");

                    if (value is null && string.IsNullOrEmpty(value))
                        throw new BuilderException($"Alias \"{cEx?.Value}\" cannot have an empty value");

                    return isSelectorParsing ? alias : value;
                }
            }
            else
                throw new BuilderException($"Unsupported expression in provided arguments");


            var memberAttributes = GetMemberAttributeArguments(memberInfo, config.ForeignKeyAttributes);

            if (translatedInnerExpression is null)
            {
                if (memberInfo is null)
                    throw new BuilderException($"Invalid method call on provided expression");

                string format = config.ColumnAccessFormat;

                if (CheckIfMethodExists(methodName, func.Functions!))
                {
                    if (!func.CheckFunctionAllowedTypes((memberInfo as PropertyInfo)!.PropertyType, methodName))
                        throw new BuilderException($"Type \"{(memberInfo as PropertyInfo)!.PropertyType.Name}\" should not be used in method \"{methodName}\"");

                    format = string.Format(func.Functions[methodName].value, format);
                }

                var memberName = memberAttributes is null
                    ? memberInfo.Name.ToCaseFormat(caseConfig)
                    : memberAttributes.FirstOrDefault().ToString();

                var result = string.Format(format, config.Scheme, type?.ToCaseFormat(caseConfig), memberName);

                if (paramName is null)
                    sb.Append(result + ",");
                else
                {
                    if (!aliases.TryAdd(paramName, result))
                        throw new BuilderException($"Alias \"{paramName}\" already exists");

                    sb.Append(result + $" AS {paramName},");
                }
            }
            else
            {
                if (CheckIfMethodExists(methodName, func.Functions!))
                {
                    string result = string.Format(func.Functions[methodName].value, translatedInnerExpression);

                    if (paramName is null)
                        sb.Append(result + ",");
                    else
                    {
                        if (!aliases.TryAdd(paramName, result))
                            throw new BuilderException($"Alias \"{paramName}\" already exists");
                        sb.Append(result + $" AS {paramName},");
                    }
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
            if (Constants.StringTypes.Contains(bExp.Type))
                throw new BuilderException($"Binary expression cannot be parsed when left or right operands have type {bExp.Type}. If you want concat strings use PConcat function instead.");

            var translator = new PostgreSqlConditionTranslator(config, func);
            string translatedBinary = translator.Translate(type!, bExp);

            if (paramName is null)
                return translatedBinary + ",";
            else
            {
                if (!aliases.TryAdd(paramName, translatedBinary))
                    throw new BuilderException($"Alias \"{paramName}\" already exists");
                return string.Format("{0} AS {1},", translatedBinary, paramName);
            }
        }

        /// <summary>
        /// Checks if method exists
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="functions"></param>
        /// <returns></returns>
        /// <exception cref="BuilderException"></exception>
        protected virtual bool CheckIfMethodExists(string? methodName, Dictionary<string, (string value, HashSet<Type> allowedTypes)> functions) =>
            methodName is not null && functions.ContainsKey(methodName)
            ? true
            : throw new BuilderException($"Funtion '{methodName}' is not registered into dictionary {nameof(func.Functions)}");


        protected override Expression VisitUnary(UnaryExpression u)
        {
            string value = func.ExpressionTypes[u.NodeType][0];
            if (!string.IsNullOrEmpty(value))
                sb.Append(value);
            Visit(u.Operand);
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (!func.ExpressionTypes.ContainsKey(b.NodeType))
                throw new BuilderException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

            sb.Append('(');
            Visit(b.Left);

            string[] values = func.ExpressionTypes[b.NodeType];
            sb.Append(values.Length > 1 ? values[IsNullConstant(b.Right) ? 0 : 1] : values[0]);

            Visit(b.Right);
            sb.Append(')');
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            sb.Append(c.Value is null ? "NULL" : Constants.StringTypes.Contains(c.Value!.GetType()) ? string.Format("'{0}'", c.Value) : c.Value);
            return c;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression is not null && (m.Expression.NodeType == ExpressionType.Parameter || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                if (m.Member.Name == "Length" && m.Expression is MemberExpression mExp && mExp.Type == typeof(string))
                {
                    var targetType = mExp.Expression?.Type?.ToCaseFormat(caseConfig);
                    sb.AppendFormat($"LENGTH({config.ColumnAccessFormat})", config.Scheme, targetType, mExp.Member.Name.ToCaseFormat(caseConfig));
                }
                else
                {
                    var targetType = m.Expression.Type;
                    sb.AppendFormat(config.ColumnAccessFormat, config.Scheme, targetType?.ToCaseFormat(caseConfig), m.Member.Name.ToCaseFormat(caseConfig));
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

            if (func.Functions.ContainsKey(m.Method.Name))
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
                "Any" => GetAnyMethodTranslated(m),
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
                    if (m.Object is MethodCallExpression mcExp && mcExp.Method.Name == "Alias")
                    {
                        sb.AppendFormat(" {0} LIKE '%{1}%' ", ParseMethodCallExpression(mcExp, isSelectorParsing: true), cArg.Value);
                    }
                    else if (m.Object is MemberExpression mExp)
                    {
                        sb.AppendFormat(" " + config.ColumnAccessFormat + " LIKE '%{3}%' ",
                            config.Scheme,
                            mExp!.Expression!.Type.ToCaseFormat(caseConfig),
                            mExp.Member.Name.ToCaseFormat(caseConfig),
                            cArg.Value);
                    }
                    else
                        throw new BuilderException("Contains Method argument is invalid");

                    return sb.ToString();
                }
                else if (m.Arguments[0] is MemberExpression mArg)
                {
                    arg = mArg.Member.Name;
                    expObj = m.Object;
                }
                else
                    throw new BuilderException("Contains Method argument is invalid");

            }
            else if (m.Arguments.Count == 2)
            {
                arg = ((MemberExpression)m.Arguments[1]).Member.Name;
                expObj = m.Arguments[0];
            }

            if (expObj is null)
                throw new BuilderException("Object should not be null");

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
                        throw new BuilderException("Invalid data");
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

                sb.Append(string.Format(" " + config.ColumnAccessFormat + " IN ({3}) ", config.Scheme, type?.ToCaseFormat(caseConfig), arg.ToCaseFormat(caseConfig), string.Join(',', items)));
                return sb.ToString();
            }

            var obj = (expObj as ConstantExpression)!;

            if ((obj.Type.IsGenericType && obj.Type.GenericTypeArguments.Length == 1 && isEnumerable) || isArray)
            {
                var genericArg = isArray ? obj.Type : obj.Type.GenericTypeArguments[0];

                object? val = obj.Value;

                if (val is null)
                    throw new BuilderException("Object value should not be null");

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
                    throw new BuilderException($"Unsupported type {genericArg?.Name}");

                return sb.ToString();
            }
            else
            {
                throw new BuilderException($"Unsupported type {obj.Type}");
            }
        }

        protected virtual string GetUpperLowerMethodTranslated(MethodCallExpression m, bool isLower)
        {
            if (m.Object is null)
                throw new BuilderException("Object should not be null");

            if (m.Object is MemberExpression memberExp)
            {
                return string.Format(" " + (isLower ? "LOWER" : "UPPER") + "(" + config.ColumnAccessFormat + ")", config.Scheme, type?.ToCaseFormat(caseConfig), memberExp.Member.Name.ToCaseFormat(caseConfig));
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
                throw new BuilderException("Object should not be null");

            if (m.Object is MemberExpression memberExp)
            {
                return string.Format(config.ColumnAccessFormat + "::text", config.Scheme, type?.ToCaseFormat(caseConfig), memberExp.Member.Name.ToCaseFormat(caseConfig));
            }
            else if (m.Object is ConstantExpression constExp)
            {
                return string.Format("'{0}' ", constExp?.Value?.ToString());
            }

            return string.Empty;
        }

        protected virtual string GetAnyMethodTranslated(MethodCallExpression m)
        {
            if (m?.Arguments is null || m.Arguments?.Count == 0)
                throw new BuilderException("Invalid caller object. It should be a generic collection with one generic parameter");

            if (m.Arguments is null || m.Arguments[0] is not MemberExpression mExp)
                throw new BuilderException("Invalid caller object. It should be a generic collection with one generic parameter");

            if (m.Arguments is not null && m.Arguments.Count > 2)
                throw new BuilderException("Invalid amount of arguments");

            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(mExp.Type);
            bool isArray = typeof(Array).IsAssignableFrom(mExp.Type);

            if (!((mExp.Type.IsGenericType && mExp.Type.GenericTypeArguments.Length == 1 && isEnumerable) || isArray))
                throw new BuilderException("Invalid caller object. It should be a generic collection with one generic parameter");

            var type = mExp.Type.GenericTypeArguments[0].ToCaseFormat(caseConfig);

            if (m.Arguments is not null && m.Arguments.Count == 2)
            {
                if (m.Arguments[1] is not LambdaExpression lExp)
                    throw new BuilderException("Invalid arguments. It should be an expression");

                var parsedExpression = new PostgreSqlConditionTranslator(config, func).Translate(mExp.Type.GenericTypeArguments[0], lExp);
                return string.Format(" EXISTS (SELECT 1 FROM {0}.{1} WHERE {2} LIMIT 1) ", config.Scheme, type, parsedExpression);
            }

            return string.Format(" EXISTS (SELECT 1 FROM {0}.{1} LIMIT 1) ", config.Scheme, type);
        }

        protected void AppendInStatement<TItem>(IEnumerable<TItem> items, string arg, StringBuilder sb)
        {
            if (items is not null && items.Any())
            {
                if (Constants.StringTypes.Contains(typeof(TItem)))
                    sb.Append(string.Format(" " + config.ColumnAccessFormat + " IN ({3}) ", config.Scheme, type?.ToCaseFormat(caseConfig), arg.ToCaseFormat(caseConfig), string.Join(',', items.Select(it => $"'{it}'"))));
                else
                    sb.Append(string.Format(" " + config.ColumnAccessFormat + " IN ({3}) ", config.Scheme, type?.ToCaseFormat(caseConfig), arg.ToCaseFormat(caseConfig), string.Join(',', items)));
            }
        }

        protected static string ConvertToItemWithType(object? item)
        {
            if (item is null)
                return string.Empty;

            if (Constants.StringTypes.Contains(item.GetType()))
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
