
namespace DataAccessLayer.Query
{

    /// <summary>
    /// Der Typ der Abfragenknoten
    /// </summary>
    public enum ConditionType
    {

        /// <summary>
        /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit einem angegebenen Wert dar
        /// </summary>
        ValueCompare,

        /// <summary>
        /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte auf NULL dar
        /// </summary>
        NullCompare,

        /// <summary>
        /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit einer anderen Spalte dar
        /// </summary>
        ColumnCompare,

        /// <summary>
        /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit angegebenen Werten dar
        /// </summary>
        IsIn,

        /// <summary>
        /// Stellt eine logische Verknüpfung mehrerer Abfrageknoten dar
        /// </summary>
        Concatenation

    }

}
