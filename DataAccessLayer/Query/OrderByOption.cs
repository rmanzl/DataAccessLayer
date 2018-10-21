
namespace RobinManzl.DataAccessLayer.Query
{

    /// <summary>
    /// Klasse, die verwendet werden kann, um die Sortierung der Zeilen bei Abfragen zu bestimmen
    /// </summary>
    public class OrderByOption
    {

        /// <summary>
        /// Kann zur Festlegung des Sortier-Attributes verwendet werden
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        /// Wird verwendet, um die Sortier-Richtung zu spezifizieren
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

        /// <summary>
        /// Standard-Konstruktor
        /// </summary>
        public OrderByOption()
        {
        }

        /// <summary>
        /// Konstrukor zur Erstellung einer OrderByOption-Instanz
        /// </summary>
        /// <param name="column">Die Angabe des Spaltennamens</param>
        /// <param name="sortDirection">Die Angabe der Sortier-Richtung</param>
        public OrderByOption(string column, SortDirection sortDirection = SortDirection.Ascending)
        {
            Column = column;
            SortDirection = sortDirection;
        }

    }

}
