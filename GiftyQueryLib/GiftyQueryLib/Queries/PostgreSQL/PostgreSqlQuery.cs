using GiftyQueryLib.Builders.PostgreSql;
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
        protected string selectAllMark = string.Empty;

        protected Expression<Func<T, object>>? exceptColumnSelector = null;

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

        public virtual IJoinNode<T> Count(Expression<Func<T, object>>? columnSelector = null)
        {
            ParseCountExpression(columnSelector);

            return this;
        }

        public IJoinNode<T> CountDistinct(Expression<Func<T, object>>? columnSelector = null)
        {
            ParseCountExpression(columnSelector, true);

            return this;
        }

        public virtual IJoinNode<T> Select(Expression<Func<T, object>> include)
        {
            ParseSelectExpression(include);

            return this;
        }

        public IJoinNode<T> SelectSingle(Expression<Func<T, object>>? columnSelector = null)
        {
            ParseSelectSingleExpression(columnSelector);

            return this;
        }

        public IJoinNode<T> SelectDistinctSingle(Expression<Func<T, object>>? columnSelector = null)
        {
            ParseSelectSingleExpression(columnSelector, true);

            return this;
        }

        public IJoinNode<T> SelectDistinct(Expression<Func<T, object>> include)
        {
            ParseSelectExpression(include, distinct: true);

            return this;
        }

        public IJoinNode<T> SelectAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null)
        {
            ParseSelectExpression(include, exclude, true);

            return this;
        }

        public IJoinNode<T> SelectDistinctAll(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null)
        {
            ParseSelectExpression(include, exclude, true, true);

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
                        var attrData = conditionTranslator.GetAttrData(property);

                        if (attrData.Value(AttrType.Key) is null && attrData.Value(AttrType.NotMapped) is null)
                        {
                            string propName;
                            var fkAttr = attrData.Value(AttrType.ForeignKey);

                            if (fkAttr is null)
                            {
                                if (property.IsCollection())
                                {
                                    var generic = property.GetGenericArg();

                                    if (Constants.StringTypes.Contains(generic!) || Constants.NumericTypes.Contains(generic!) || attrData.Value(AttrType.Json) is not null)
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
                                    propName = fkAttr.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat(caseConfig);
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

                var valuesSql = valuesSb.TrimEndComma();

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

            var pairsSql = pairs.TrimEndComma();

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

                var valuesSql = valuesSb.TrimEndComma();

                valuesSetSb.AppendFormat("({0}), ", valuesSb.TrimEndComma());
            }

            var valuesSetSql = valuesSetSb.TrimEndComma();
            var propsSql = propsSb.TrimEndComma();
            var pairsSql = pairsSb.TrimEndComma();

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
            if (!string.IsNullOrEmpty(selectAllMark) || exceptColumnSelector is not null)
                str = str.Replace(selectAllMark, string.Empty);

            return str;
        }

        private void ParseCountExpression(Expression<Func<T, object>>? columnSelector = null, bool distinct = false)
        {
            string sql = "SELECT ";
            string sqlDistinct = distinct ? "DISTINCT " : "";
            string sqlCount = "COUNT({0}) ";
            string? name;

            if (columnSelector is not null)
            {
                var data = conditionTranslator.GetMemberData(columnSelector);
                var info = data.MemberInfo;
                var type = data.CallerType?.ToCaseFormat(caseConfig);
                name = info?.Name?.ToCaseFormat(caseConfig);
                sqlCount = string.Format(sqlCount, string.Format(config.ColumnAccessFormat, config.Scheme, type, name));
            }
            else
            {
                name = "all";
                sqlCount = string.Format(sqlCount, "*");
            }

            conditionTranslator.AddAutoGeneratedAlias(Guid.NewGuid().ToString(), sqlCount);

            sqlCount = string.Format(sqlCount + " AS {0} ", $"count_{name}");

            value = new StringBuilder(sql + sqlDistinct + sqlCount);
        }

        private void ParseSelectSingleExpression(Expression<Func<T, object>>? columnSelector = null, bool distinct = false)
        {
            string sql = "SELECT ";
            string sqlDistinct = distinct ? "DISTINCT " : "";

            if (columnSelector is null)
            {
                value = new StringBuilder(sql + sqlDistinct + "1 ");
            }
            else
            {
                var data = conditionTranslator.GetMemberData(columnSelector);
                var info = data.MemberInfo;
                var type = data.CallerType?.ToCaseFormat(caseConfig);
                var name = info?.Name?.ToCaseFormat(caseConfig);

                value = new StringBuilder(sql + sqlDistinct + string.Format(config.ColumnAccessFormat, config.Scheme, type, name) + " ");
            }
        }

        private void ParseSelectExpression(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool selectAll = false, bool distinct = false)
        {
            string sql = "SELECT ";

            selectAllMark = selectAll ? $"<s_a_{Guid.NewGuid()}>" : string.Empty;

            var parsedSelector = conditionTranslator.ParseAnonymousSelector(include, exclude, config: new SelectorConfig { SelectAll = selectAll });

            string sqlDistinct = distinct ? "DISTINCT " : "";
            string sqlRows = parsedSelector.Result! + (selectAll ? selectAllMark : "");
            string sqlFrom = string.Format(" FROM {0}.{1} ", config.Scheme, typeof(T).ToCaseFormat(caseConfig));

            value = new StringBuilder(sql + sqlDistinct + sqlRows + sqlFrom);
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

            if (!string.IsNullOrEmpty(selectAllMark))
            {
                string sqlRows = "," + conditionTranslator.ParseAnonymousSelector(null, exceptColumnSelector, memberType, new SelectorConfig { SelectAll = true }).Result! + selectAllMark;
                value = new StringBuilder(value.ToString().Replace(selectAllMark, sqlRows));
            }
        }

        public IHavingNode<T> Group(Expression<Func<T, object>> include, Expression<Func<T, object>>? exclude = null)
        {
            var parsed = conditionTranslator.ParseAnonymousSelector(include, exclude, config: new SelectorConfig
            {
                AllowMethodCall = false,
                AllowBinary = false,
                UseAliases = false
            });
            value.AppendFormat("GROUP BY {0} ", parsed.Result);

            return this;
        }

        public IOrderNode<T> Having(Expression<Func<T, bool>> condition)
        {
            value.AppendFormat("HAVING {0} ", conditionTranslator.Translate<T>(condition));
            return this;
        }

        public IOrderNode<T> Order(Expression<Func<T, object>> columnSelector, OrderType orderType = OrderType.Asc)
        {
            if (columnSelector is null)
                throw new BuilderException("Row selector must be provided");

            var parsed = conditionTranslator.ParseAnonymousSelector(columnSelector, config: new SelectorConfig
            {
                AllowBinary = false,
                AllowConstant = false
            });

            var orderRowsSql = string.Join(',', parsed.Result!.Split(',').Select(it => it + " " + orderType.ToString().ToUpper()));
            value.AppendFormat("ORDER BY {0} ", orderRowsSql);

            return this;
        }
    }
}
