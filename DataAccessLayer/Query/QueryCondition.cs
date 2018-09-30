using System.Collections.Generic;
using System.Text;

namespace RobinManzl.DataAccessLayer.Query
{

    /// <summary>
    /// Stellt die Basis aller Abfragemöglichkeiten dar
    /// </summary>
    public abstract class QueryCondition
    {
        
        /// <summary>
        /// Gibt den Typ der Bedingung an
        /// </summary>
        public abstract ConditionType ConditionType { get; }

        internal abstract void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters);

    }

}
