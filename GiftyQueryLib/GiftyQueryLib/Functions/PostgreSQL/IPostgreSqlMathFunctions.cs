namespace GiftyQueryLib.Functions.PostgreSQL
{
    /// <summary>
    /// Represents PostgreSQL-specific Math Functions
    /// </summary>
    public interface IPostgreSqlMathFunctions
    {
        /// <summary>
        /// Transforms into <b>CEIL(value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Ceil(double _) => default;

        /// <summary>
        /// Transforms into <b>COT(value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Cot(double _) => default;

        /// <summary>
        /// Transforms into <b>DEGREES(value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Degrees(double _) => default;

        /// <summary>
        /// Transforms into <b>DIV(n_value, m_value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public int Div(int n, int m) => default;

        /// <summary>
        /// Transforms into <b>EXP(value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Exp(double _) => default;

        /// <summary>
        /// Transforms into <b>PI()</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Pi() => default;

        /// <summary>
        /// Transforms into <b>RADIANS(value)</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Radians(double _) => default;

        /// <summary>
        /// Transforms into <b>RANDOM()</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Random() => default;

        /// <summary>
        /// Transforms into <b>TRUNC(n_value, [d_value])</b> sql math function
        /// </summary>
        /// <returns></returns>
        public double Trunc(double n, int d = 1) => default;
    }
}
