using GiftyQueryLib.Utils;
using System.Linq.Expressions;

namespace GiftyQueryLib.Functions
{
    public class PostgreSqlFunctions
    {
        #region Functions

        /// <summary>
        /// Transforms into <b>COUNT(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public int Count(object _) => default;

        /// <summary>
        /// Transforms into <b>MIN(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Min(object _) => default;

        /// <summary>
        /// Transforms into <b>MAX(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Max(object _) => default;

        /// <summary>
        /// Transforms into <b>AVG(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Avg(object _) => default;

        /// <summary>
        /// Transforms into <b>SUM(coumn_name)</b> sql aggregate function
        /// </summary>
        /// <returns></returns>
        public double Sum(object _) => default;

        /// <summary>
        /// Transforms into <b>CONCAT(str1, str2, ...)</b> sql concatenation function
        /// </summary>
        /// <returns></returns>
        public string Concat(params object[] _) => string.Empty;

        /// <summary>
        /// Gets alias value by it's name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T? Alias<T>(string name) => default;

        #endregion

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
        public Dictionary<string, (string value, HashSet<Type>? allowedTypes)> Functions => new()
        {
            { nameof(Count), ("COUNT({0})", null) },
            { nameof(Sum), ("SUM({0})", Constants.NumericTypes) },
            { nameof(Avg), ("AVG({0})", Constants.NumericTypes) },
            { nameof(Min), ("MIN({0})", Constants.NumericTypes) },
            { nameof(Max), ("MAX({0})", Constants.NumericTypes) },
            { nameof(Concat), ("CONCAT({0})", null) },
            { nameof(Alias), ("HAVING {0}", null) }
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
