using GiftyQueryLib.Utils;
using System.Linq.Expressions;

namespace GiftyQueryLib.Functions
{
    public class PostgreSqlFunctions
    {
        private static PostgreSqlFunctions? instance = null;
        private PostgreSqlFunctions() { }

        /// <summary>
        /// Get Instance of PostgreSqlFunctions class
        /// </summary>
        public static PostgreSqlFunctions Instance => instance ??= new PostgreSqlFunctions();
        
        #region Dictionaries

        /// <summary>
        /// PostgreSQL Expression Types
        /// </summary>
        public Dictionary<ExpressionType, string[]> ExpressionTypes => new()
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

        /// <summary>
        /// PostgreSQL Functions Syntaxes
        /// </summary>
        public Dictionary<string, (string value, HashSet<Type?>? allowedTypes)> Functions => new()
        {
            { "Count", ("COUNT({0})", null) },
            { "Sum", ("SUM({0})", Constants.NumericTypes) },
            { "Avg", ("AVG({0})", Constants.NumericTypes) },
            { "Min", ("MIN({0})", Constants.NumericTypes) },
            { "Max", ("MAX({0})", Constants.NumericTypes) },
            { "Concat", ("CONCAT({0})", null) },
            { "Alias", ("", null) }
        };

        #endregion

        #region Utils

        /// <summary>
        /// Check if specified type contains inside function allowed types set
        /// </summary>
        /// <param name="source"></param>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public bool CheckFunctionAllowedTypes(Type? source, string funcName)
        {
            var func = Functions.TryGetValue(funcName, out var result);

            if (!func || source is null)
                return false;

            return result.allowedTypes == null || result.allowedTypes?.Contains(source) == true;
        }

        #endregion
    }
}
