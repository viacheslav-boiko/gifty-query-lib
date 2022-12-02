using GiftyQueryLib.Config;
using GiftyQueryLib.Enums;
using GiftyQueryLib.Queries.QueryNodes;
using GiftyQueryLib.Translators;
using GiftyQueryLib.Translators.Models;
using GiftyQueryLib.Translators.SqlTranslators;
using GiftyQueryLib.Utils;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using C = GiftyQueryLib.Config.QueryConfig;

namespace GiftyQueryLib.Queries
{
    public class PostgreSqlQuery<T> :
        IInstructionNode<T>, IConditionNode<T>, IEditConditionNode<T>, IWhereNode<T>, IOrderNode<T>,
        IKeySetPaginationNode<T>, ILimitNode, IJoinNode<T>,
        IGroupNode<T> where T : class
    {
        protected StringBuilder value = new(string.Empty);

        protected bool whereIsUsed = false;
        protected bool selectAllIsUsed = false;
        protected Expression<Func<T, object>>? exceptRowSelector = null;

        private readonly BaseConditionTranslator conditionTranslator;

        private PostgreSqlQuery(BaseConditionTranslator conditionTranslator)
        {
            this.conditionTranslator = conditionTranslator;
        }

        public static IInstructionNode<T> Flow()
        {
            return new PostgreSqlQuery<T>(new PostgreSqlConditionTranslator());
        }

        public virtual IConditionNode<T> Select(Expression<Func<T, object>>? include = null, Expression<Func<T, object>>? exclude = null, bool distinct = false)
        {
            string sql = "SELECT ";
            string sqlDistinct = distinct ? "DISTINCT " : string.Empty;

            SelectorData parsedSelector = conditionTranslator.ParseAnonymousSelector(include, exclude);

            selectAllIsUsed = parsedSelector.ExtraData?.IsSelectAll;
            exceptRowSelector = exclude;

            string sqlRows = parsedSelector.Result! + (selectAllIsUsed ? "{0}" : "");
            string sqlFrom = string.Format(" FROM {0}.{1} ", C.Scheme, typeof(T).ToCaseFormat());

            value = new StringBuilder(sql + sqlDistinct + sqlRows + sqlFrom);

            return this;
        }

        public virtual IQueryStringBuilder Insert(params T[] entities)
        {
            if (entities is null || entities.Length == 0)
            {
                throw new ArgumentException("At least one parameter should be provided");
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
                            if (C.KeyAttributes.Any(type => attr.AttributeType == type)) keyAttrData = attr;
                            if (C.NotMappedAttributes.Any(type => attr.AttributeType == type)) notMappedAttrData = attr;
                            if (C.ForeignKeyAttributes.Any(type => attr.AttributeType == type)) foreignKeyAttrData = attr;
                        }

                        if (keyAttrData is null && notMappedAttrData is null)
                        {
                            var propName = foreignKeyAttrData is null
                                ? property.Name.ToCaseFormat()
                                : foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat();

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
                        .FirstOrDefault(attr => C.ForeignKeyAttributes.Any(type => attr.AttributeType == type));

                    var propName = foreignKeyAttrData is null
                               ? property.Name.ToCaseFormat()
                               : foreignKeyAttrData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat();

                    propName = string.Format("{0}", propName);

                    if (nonNullableProps.Contains(propName))
                    {
                        object? value = null;

                        if (foreignKeyAttrData is not null)
                        {
                            var keyProp = property.PropertyType
                                .GetProperties().FirstOrDefault(prop => prop.GetCustomAttributes(true)
                                    .FirstOrDefault(attr => C.KeyAttributes.Any(it => attr.GetType() == it)) is not null);

                            if (keyProp is null)
                                throw new ArgumentException($"The related table class {property.Name} does not contain key attribute");

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

            value = new StringBuilder(string.Format("INSERT INTO {0}.{1} ({2}) VALUES {3}", C.Scheme, typeof(T).ToCaseFormat(), columnsSql, rowsSql));

            return this;
        }

        public virtual IEditConditionNode<T> Update(params T[] entities)
        {
            // TODO
            return this;
        }

        public virtual IEditConditionNode<T> Delete()
        {
            value = new StringBuilder(string.Format("DELETE FROM {0}.{1} ", C.Scheme, typeof(T).ToCaseFormat()));

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

        public virtual IOrderNode<T> Order(params (Expression<Func<T, object>> rowSelector, OrderType orderType)[] rowsForOrdering)
        {
            if (rowsForOrdering is null || !rowsForOrdering.Any())
                throw new ArgumentException("Method must accept at least one ordering pair");

            string sqlOrder = "ORDER BY ";

            var pairs = new List<string>();

            foreach (var (rowSelector, orderType) in rowsForOrdering)
            {
                var data = conditionTranslator.GetMemberData(rowSelector);
                string sqlOrderField = string.Format(C.ColumnAccessFormat + " ", C.Scheme, data.CallerType!.ToCaseFormat(), data.MemberInfo?.Name.ToCaseFormat());

                string sqlDirection = Enum.GetName(typeof(OrderType), (int)orderType)!;

                pairs.Add(sqlOrderField + sqlDirection);
            }

            value.Append(sqlOrder + string.Join(',', pairs) + " ");

            return this;
        }

        public virtual ILimitNode Limit(int limit)
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
            var keyProp = memberType
                .GetProperties().FirstOrDefault(prop => prop.GetCustomAttributes(true).FirstOrDefault(attr => attr is KeyAttribute) is not null);

            if (keyProp is null)
                throw new ArgumentException($"Key attribute is absent in related table");

            if (!memberType.IsClass)
                throw new ArgumentException($"Unable to join using non-reference properties");

            string expresstionTypeName = selectorData.CallerType!.ToCaseFormat();
            string memberTypeName = memberType.Name.ToCaseFormat();
            string keyPropName = keyProp.Name.ToCaseFormat();

            var foreignKeyAttributeData = selectorData.MemberInfo?
                .GetCustomAttributesData().FirstOrDefault(it => C.ForeignKeyAttributes.Any(attr => it.AttributeType == attr));

            string foreignKeyName = string.Empty;

            if (foreignKeyAttributeData is null)
            {
                foreignKeyName = string.Format(C.ColumnAccessFormat, C.Scheme, expresstionTypeName, memberTypeName + "_" + keyPropName);
            }
            else
            {
                foreignKeyName = string.Format(C.ColumnAccessFormat, C.Scheme, expresstionTypeName, foreignKeyAttributeData.ConstructorArguments[0].Value!.ToString()!.ToCaseFormat());
            }

            string inner = string.Format(C.ColumnAccessFormat, C.Scheme, memberTypeName, keyPropName);
            string sqlJoinType = joinType.ToString().ToUpper();

            value.Append(string.Format("{0} JOIN {1}.{2} ON {3} = {4} ", sqlJoinType, C.Scheme, memberTypeName, foreignKeyName, inner));

            if (selectAllIsUsed)
            {
                string sqlRows = "," + conditionTranslator.ParseAnonymousSelector(it => new { SelectType.All }, exceptRowSelector, memberType).Result! + "{0}";
                value = new StringBuilder(string.Format(value.ToString(), sqlRows));
            }
        }
    }
}
