using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Exceptions;
using GiftyQueryLib.Functions;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Translators.SqlTranslators;
using GiftyQueryLib.Utils;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text;

namespace GiftyQueryLib.Queries.PostgreSQL
{
    public class PostgreSqlQuery<T> :
       IInstructionNode<T>, IEditConditionNode<T>, IJoinNode<T>, IWhereNode<T>, IGroupNode<T>, IHavingNode<T>, IOrderNode<T>, ILimitNode, IOffsetNode where T : class
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

        public virtual IJoinNode<T> Count(Expression<Func<T, object>>? rowSelector = null, bool distinct = false)
        {
            string sql = "SELECT ";
            string sqlDistinct = distinct ? "DISTINCT " : string.Empty;
            string sqlCount = "COUNT({0}) AS {1} ";

            if (rowSelector is not null)
            {
                var data = conditionTranslator.GetMemberData(rowSelector);
                var info = data.MemberInfo;
                var type = data.CallerType?.ToCaseFormat(caseConfig);
                var name = info?.Name?.ToCaseFormat(caseConfig);
                sqlCount = string.Format(sqlCount, string.Format(config.ColumnAccessFormat, config.Scheme, type, name), "count_" + name);
            }
            else
            {
                sqlCount = string.Format(sqlCount, "*", "count_all");
            }

            value = new StringBuilder(sql + sqlDistinct + sqlCount);

            return this;
        }

        public virtual IJoinNode<T> Select(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false)
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
                throw new BuilderException("At least one entity should be provided");

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
                        var (keyAttrData, notMappedAttrData, foreignKeyAttrData) = conditionTranslator.GetAttrData(property);

                        if (keyAttrData is null && notMappedAttrData is null)
                        {
                            string propName;
                            if (foreignKeyAttrData is null)
                            {
                                if (property.IsCollection())
                                {
                                    var generic = property.GetGenericArg();

                                    if (Constants.StringTypes.Contains(generic!) || Constants.NumericTypes.Contains(generic!))
                                        propName = property.Name.ToCaseFormat(caseConfig);
                                    else
                                        continue;
                                }
                                else
                                    propName = property.Name.ToCaseFormat(caseConfig);
                            }
                            else
                            {
                                if (property.IsCollection())
                                {
                                    var generic = property.GetGenericArg();

                                    if (Constants.StringTypes.Contains(generic!) || Constants.NumericTypes.Contains(generic!))
                                        throw new BuilderException("Primitive-typed collection should not be marked with foreign key attributes");
                                    else
                                    {
                                        // Handle many to many
                                        continue;
                                    }
                                }
                                else
                                    propName = foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig);
                            }

                            nonNullableProps.Add(string.Format("{0}", propName));
                        }
                    }
                }
            }

            foreach (T entity in entities)
            {
                var valuesSb = new StringBuilder();
                var entityData = conditionTranslator.GetPopertyWithValueOfEntity(entity);
                entityData.ToList().ForEach(pair =>
                {
                    if (!nonNullableProps.Contains(pair.Key))
                        return;

                    var value = pair.Value;
                    var parsedValue = value is null
                        ? "NULL" : string.Format(Constants.StringTypes.Contains(value.GetType())
                            ? "'{0}'" : "{0}", value.ToString());

                    valuesSb.Append(parsedValue + ", ");
                });

                var valuesSql = valuesSb.ToString();
                valuesSql = valuesSql.Remove(valuesSql.Length - 2);

                values.Add(string.Format("({0})", valuesSql));
            }

            string columnsSql = string.Join(',', nonNullableProps);
            string rowsSql = string.Join(',', values);

            value = new StringBuilder(string.Format("INSERT INTO {0}.{1} ({2}) VALUES {3}", config.Scheme, typeof(T).ToCaseFormat(caseConfig), columnsSql, rowsSql));

            return this;
        }

        public virtual IEditConditionNode<T> Update(object entity)
        {
            var entityData = conditionTranslator.GetPopertyWithValueOfEntity(entity);
            var pairs = new StringBuilder();
            var type = typeof(T).ToCaseFormat(caseConfig);

            entityData.ToList().ForEach(pair =>
            {
                var value = pair.Value;
                var parsedValue = value is null
                    ? "NULL" : string.Format(Constants.StringTypes.Contains(value.GetType())
                        ? "'{0}'" : "{0}", value.ToString());

                pairs.AppendFormat(config.ColumnAccessFormat + " = {3}, ", config.Scheme, type, pair.Key, parsedValue);
            });

            string pairsSql = pairs.ToString();
            pairsSql = pairsSql.Remove(pairsSql.Length - 2);

            value = new StringBuilder(string.Format("UPDATE {0}.{1} SET {2} ", config.Scheme, type, pairsSql));

            return this;
        }

        public virtual IQueryStringBuilder UpdateRange(params object[] entities)
        {
            if (entities is null || entities.Length == 0)
                throw new BuilderException("Entity list cannot be null or empty");

            var valuesSetSb = new StringBuilder();
            var propsSb = new StringBuilder();
            var pairsSb = new StringBuilder();
            var tempTable = $"tmp_{Guid.NewGuid().ToString().Split('-')[0]}";
            var type = typeof(T).ToCaseFormat(caseConfig);

            bool propsWritten = false;

            foreach (var entity in entities)
            {
                var valuesSb = new StringBuilder();
                var entityData = conditionTranslator.GetPopertyWithValueOfEntity(entity, false);

                entityData.ToList().ForEach(pair =>
                {
                    if (!propsWritten)
                    {
                        var prop = pair.Key.ToCaseFormat(caseConfig);
                        propsSb.Append(prop + ", ");
                        pairsSb.AppendFormat(config.ColumnAccessFormat + " = {3}, ", config.Scheme,
                            type, prop, tempTable + "." + prop);
                    }

                    var value = pair.Value;
                    var parsedValue = value is null
                        ? "NULL" : string.Format(Constants.StringTypes.Contains(value.GetType())
                            ? "'{0}'" : "{0}", value.ToString());

                    valuesSb.Append(parsedValue + ", ");
                });

                propsWritten = true;

                var valuesSql = valuesSb.ToString();
                valuesSql = valuesSql.Remove(valuesSql.Length - 2);

                valuesSetSb.AppendFormat("({0}), ", valuesSql);
            }

            var valuesSetSql = valuesSetSb.ToString();
            valuesSetSql = valuesSetSql.Remove(valuesSetSql.Length - 2);

            var propsSql = propsSb.ToString();
            propsSql = propsSql.Remove(propsSql.Length - 2);

            var pairsSql = pairsSb.ToString();
            pairsSql = pairsSql.Remove(pairsSql.Length - 2);

            value = new StringBuilder(string.Format("UPDATE {0}.{1} SET {2} FROM (VALUES {3}) AS {4} ({5})",
                config.Scheme, type, pairsSql, valuesSetSql, tempTable, propsSql));

            return this;
        }

        public virtual IEditConditionNode<T> Delete()
        {
            value = new StringBuilder(string.Format("DELETE FROM {0}.{1} ", config.Scheme, typeof(T).ToCaseFormat(caseConfig)));

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
