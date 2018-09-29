namespace RobinManzl.DataAccessLayer.Query
{

    /// <summary>
    /// Der Vergleichsoperator, der für einen Wert- oder Spaltenvergleich verwendet werden soll
    /// </summary>
    public enum Operator
    {

        /// <summary>
        /// Prüft auf Gleichheit
        /// </summary>
        Equals,

        /// <summary>
        /// Größer als
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Größer als oder gleich groß
        /// </summary>
        GreaterThanOrEquals,

        /// <summary>
        /// Kleiner als
        /// </summary>
        SmallerThan,

        /// <summary>
        /// Kleiner als oder gleich groß
        /// </summary>
        SmallerThanOrEquals,

        /// <summary>
        /// Prüft auf Ungleichheit
        /// </summary>
        NotEquals,

        /// <summary>
        /// Verwendet den Like-Operator
        /// </summary>
        Like,

        /// <summary>
        /// Verwendet den Not-Like-Operator
        /// </summary>
        NotLike

    }

}
