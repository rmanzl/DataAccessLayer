
namespace DataAccessLayer.Query
{

    /// <summary>
    /// Diese Klasse kann verwendet werden, um der <code>GetEntities</code>-Methode der <code>DbService</code>-Klasse Optionen anzugeben
    /// </summary>
    public class QueryOptions
    {

        /// <summary>
        /// Die Angabe der maximal zurückzuliefernden Zeilen
        /// </summary>
        public int? MaxRowCount { get; set; }

        /// <summary>
        /// Gibt das erste Attribut für eine Sortierung an
        /// </summary>
        public string OrderByColumn1 { get; set; }

        /// <summary>
        /// Gibt das zweite Attribut für eine Sortierung an
        /// </summary>
        public string OrderByColumn2 { get; set; }

        /// <summary>
        /// Gibt das dritte Attribut für eine Sortierung an
        /// </summary>
        public string OrderByColumn3 { get; set; }

        /// <summary>
        /// Gibt an, ob die Sortierung absteigend durchgeführt werden soll
        /// </summary>
        public bool OrderDescending { get; set; }

    }

}
