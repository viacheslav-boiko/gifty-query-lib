using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Exceptions;
using GiftyQueryLib.Functions;
using GiftyQueryLib.Translators;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Translators.SqlTranslators;
using GiftyQueryLib.Utils;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GiftyQueryLib.Queries.PostgreSQL
{
    public class PostgreSqlQuery<T> :
        IInstructionNode<T>, IEditConditionNode<T>, IConditionNode<T>, IJoinNode<T>, IWhereNode<T>, IGroupNode<T>, IHavingNode<T>, IOrderNode<T>, ILimitNode, IOffsetNode where T : class
    {
        protected StringBuilder value = new(string.Empty);

        protected bool whereIsUsed = false;
        protected bool selectAllIsUsed = false;
        protected bool groupByIsUsed = false;

        protected Expression<Func<T, object>>? exceptRowSelector = null;

        private readonly PostgreSqlConditionTranslator conditionTranslator;
        private readonly PostgreSqlConfig config;
        private readonly CaseFormatterConfig caseConfig;

        private PostgreSqlQuery(PostgreSqlConditionTranslator conditionTranslator, PostgreSqlConfig config)
        {
            this.conditionTranslator = conditionTranslator;
            this.config = config;
            this.caseConfig = new CaseFormatterConfig { CaseType = config.CaseType, CaseFormatterFunc = config.CaseFormatterFunc };
        }

        public static IInstructionNode<T> Flow(PostgreSqlConfig config, PostgreSqlFunctions func)
        {
            return new PostgreSqlQuery<T>(new PostgreSqlConditionTranslator(config, func), config);
        }

        public virtual IConditionNode<T> Select(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false)
        {
            string sql = "SELECT ";
            string sqlDistinct = distinct ? "DISTINCT " : string.Empty;

            SelectorData parsedSelector = conditionTranslator.ParseAnonymousSelector(include, exclude);

            selectAllIsUsed = parsedSelector.ExtraData?.IsSelectAll;
            exceptRowSelector = exclude;

            string sqlRows = parsedSelector.Result! + (selectAllIsUsed ? "{0}" : "");
            string sqlFrom = string.Format(" FROM {0}.{1} ", config.Scheme, typeof(T).ToCaseFormat(caseConfig));

            value = new StringBuilder(sql + sqlDistinct + sqlRows + sqlFrom);

            return this;
        }

        public virtual IQueryStringBuilder Insert(params T[] entities)
        {
            if (entities is null || entities.Length == 0)
            {
                throw new BuilderException("At least one parameter should be provided");
            }

            var values = new List<string>();
            var nonNullableProps = new HashSet<string>();

            foreach (T entity in entities)
            {
                var props = entity.GetType().GetProperties();

                foreach (var property in props)
                {
                    var value = property.GetValue(entity);

                    if (value is not null)
                    {
                        var attributeData = property.GetCustomAttributesData();

                        CustomAttributeData? keyAttrData = null;
                        CustomAttributeData? notMappedAttrData = null;
                        CustomAttributeData? foreignKeyAttrData = null;

                        foreach (var attr in attributeData)
                        {
                            if (config.KeyAttributes.Any(type => attr.AttributeType == type)) keyAttrData = attr;
                            if (config.NotMappedAttributes.Any(type => attr.AttributeType == type)) notMappedAttrData = attr;
                            if (config.ForeignKeyAttributes.Any(type => attr.AttributeType == type)) foreignKeyAttrData = attr;
                        }

                        if (keyAttrData is null && notMappedAttrData is null)
                        {
                            var propName = foreignKeyAttrData is null
                                ? property.Name.ToCaseFormat(caseConfig)
                                : foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig);

                            nonNullableProps.Add(string.Format("{0}", propName));
                        }
                    }
                }
            }

            foreach (T entity in entities)
            {
                var props = entity.GetType().GetProperties();
                var subvalues = new List<string>();

                foreach (var property in props)
                {
                    var foreignKeyAttrData = property.GetCustomAttributesData()
                        .FirstOrDefault(attr => config.ForeignKeyAttributes.Any(type => attr.AttributeType == type));

                    var propName = foreignKeyAttrData is null
                               ? property.Name.ToCaseFormat(caseConfig)
                               : foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig);

                    propName = string.Format("{0}", propName);

                    if (nonNullableProps.Contains(propName))
                    {
                        object? value = null;

                        if (foreignKeyAttrData is not null)
                        {
                            var keyProp = property.PropertyType
                                .GetProperties().FirstOrDefault(prop => prop.GetCustomAttributes(true)
                                    .FirstOrDefault(attr => config.KeyAttributes.Any(it => attr.GetType() == it)) is not null);

                            if (keyProp is null)
                                throw new BuilderException($"The related table class {property.Name} does not contain key attribute");

                            var keyPropValue = property.GetValue(entity);

                            value = keyProp.GetValue(keyPropValue);
                        }
                        else
                        {
                            value = property.GetValue(entity);
                        }

                        if (value is null)
                        {
                            subvalues.Add("NULL");
                        }
                        else
                        {
                            var type = value.GetType();
                            if (Constants.TypesToStringCast.Contains(type))
                                subvalues.Add(string.Format("'{0}'", value.ToString()));
                            else
                                subvalues.Add(string.Format("{0}", value.ToString()));
                        }
                    }
                }

                values.Add(string.Format("({0})", string.Join(',', subvalues)));
            }

            string columnsSql = string.Join(',', nonNullableProps);
            string rowsSql = string.Join(',', values);

            value = new StringBuilder(string.Format("INSERT INTO {0}.{1} ({2}) VALUES {3}", config.Scheme, typeof(T).ToCaseFormat(caseConfig), columnsSql, rowsSql));

            return this;
        }

        public virtual IEditConditionNode<T> Update(params T[] entities)
        {
            // TODO
            return this;
        }

        public virtual IEditConditionNode<T> Delete()
        {
            value = new StringBuilder(string.Format("DELETE FROM {0}.{1} ", config.Scheme, typeof(T).ToCaseFormat(caseConfig)));

            return this;
        }

        public virtual IJoinNode<T> Count(Expression<Func<T, object>>? rowSelector = null, CountType countType = CountType.Count)
        {
            if (countType != CountType.Count && rowSelector is null)
            {
                //TODO: Text for exception
                throw new Exception("");
            }

            if (rowSelector is null)
            {

            }

            return this;
        }

        public virtual IJoinNode<T> Join(Expression<Func<T, object>> selector, JoinType joinType = JoinType.Inner)
        {
            var selectorData = conditionTranslator.GetMemberData(selector);
            ParseJoinExpression(selectorData, joinType);

            return this;
        }

        public virtual IJoinNode<T> Join<U>(Expression<Func<U, object>> selector, JoinType joinType = JoinType.Inner)
        {
            var selectorData = conditionTranslator.GetMemberData(selector);
            ParseJoinExpression(selectorData, joinType);

            return this;
        }

        public virtual IWhereNode<T> Where(Expression<Func<T, bool>> condition)
        {
            value.Append(string.Format(whereIsUsed ? " AND {0} " : "WHERE {0} ", conditionTranslator.Translate<T>(condition)));

            whereIsUsed = true;

            return this;
        }

        public virtual IQueryStringBuilder Predicate(Expression<Func<T, bool>> condition)
        {
            value.Append(string.Format(whereIsUsed ? " AND {0} " : "WHERE {0} ", conditionTranslator.Translate<T>(condition)));

            whereIsUsed = true;

            return this;
        }

        public virtual IOffsetNode Limit(int limit)
        {
            value.Append(string.Format("LIMIT {0} ", limit));

            return this;
        }

        public virtual IQueryStringBuilder Offset(int skipped)
        {
            value.Append(string.Format("OFFSET {0} ", skipped));

            return this;
        }

        public virtual string Build()
        {
            string str = value.ToString();
            if (selectAllIsUsed || exceptRowSelector is not null)
                str = string.Format(str, string.Empty);

            return str;
        }

        private void ParseJoinExpression(MemberData selectorData, JoinType joinType)
        {
            var memberType = selectorData.MemberType;
            var keyProp = memberType?
                .GetProperties().FirstOrDefault(prop => prop.GetCustomAttributes(true).FirstOrDefault(attr => attr is KeyAttribute) is not null);

            if (keyProp is null)
                throw new BuilderException($"Key attribute is absent in related table");

            if (memberType == null || !memberType.IsClass)
                throw new BuilderException($"Unable to join using non-reference properties");

            string expresstionTypeName = selectorData.CallerType!.ToCaseFormat(caseConfig);
            string memberTypeName = memberType.Name.ToCaseFormat(caseConfig);
            string keyPropName = keyProp.Name.ToCaseFormat(caseConfig);

            var foreignKeyAttributeData = selectorData.MemberInfo?
                .GetCustomAttributesData().FirstOrDefault(it => config.ForeignKeyAttributes.Any(attr => it.AttributeType == attr));

            string foreignKeyName = string.Empty;

            if (foreignKeyAttributeData is null)
            {
                foreignKeyName = string.Format(config.ColumnAccessFormat, config.Scheme, expresstionTypeName, memberTypeName + "_" + keyPropName);
            }
            else
            {
                foreignKeyName = string.Format(config.ColumnAccessFormat, config.Scheme, expresstionTypeName, foreignKeyAttributeData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig));
            }

            string inner = string.Format(config.ColumnAccessFormat, config.Scheme, memberTypeName, keyPropName);
            string sqlJoinType = joinType.ToString().ToUpper();

            value.AppendFormat("{0} JOIN {1}.{2} ON {3} = {4} ", sqlJoinType, config.Scheme, memberTypeName, foreignKeyName, inner);

            if (selectAllIsUsed)
            {
                string sqlRows = "," + conditionTranslator.ParseAnonymousSelector(it => new { SelectType.All }, exceptRowSelector, memberType).Result! + "{0}";
                value = new StringBuilder(string.Format(value.ToString(), sqlRows));
            }
        }

        public IHavingNode<T> Group(Expression<Func<T, object>> include, Expression<Func<T, object>>? exclude = null)
        {
            groupByIsUsed = true;
            var parsed = conditionTranslator.ParseAnonymousSelector(include, exclude, null, true, false, false, true, false);
            value.AppendFormat("GROUP BY {0} ", parsed.Result);

            return this;
        }

        public IOrderNode<T> Having(Expression<Func<T, bool>> condition)
        {
            if (!groupByIsUsed)
                throw new BuilderException("Group by unique value must be used with having statement");
            value.AppendFormat("HAVING {0} ", conditionTranslator.Translate<T>(condition));
            return this;
        }

        public IOrderNode<T> Order(Expression<Func<T, object>> rowSelector, OrderType orderType = OrderType.Asc)
        {
            if (rowSelector is null)
                throw new BuilderException("Row selector must be provided");

            var parsed = conditionTranslator.ParseAnonymousSelector(rowSelector, null, null, true, true, false, false, false);

            var orderRowsSql = string.Join(',', parsed.Result!.Split(',').Select(it => it + " " + orderType.ToString().ToUpper()));
            value.AppendFormat("ORDER BY {0} ", orderRowsSql);

            return this;
        }
    }
}
