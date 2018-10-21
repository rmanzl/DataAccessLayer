
using System.Collections.Generic;

namespace RobinManzl.DataAccessLayer.Query
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
        /// Wird zur Angabe der Sortier-Optionen verwendet
        /// </summary>
        public List<OrderByOption> OrderByOptions { get; } = new List<OrderByOption>();

    }

}
