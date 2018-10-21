
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

    }

}
