using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine logische Verkettung mehrerer Abfrageknoten dar
    /// </summary>
    public sealed class SeriesCondition : QueryCondition
    {

        /// <inheritdoc />
        public override ConditionType ConditionType => ConditionType.Concatenation;

        /// <summary>
        /// Der Knoten, welche für eine Verkettung verwendet werden sollen
        /// </summary>
        public List<QueryCondition> Conditions { get; set; }

        /// <summary>
        /// Der logische Operator, welcher für die Verkettung verwendet werden soll
        /// </summary>
        public LogicalOperator LogicalOperator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            stringBuilder.Append("(");

            foreach (var condition in Conditions)
            {
                condition.GenerateConditionString(stringBuilder, parameters, tableName);

                stringBuilder.Append(" ");
                stringBuilder.Append(LogicalOperator.ToString().ToUpper());
                stringBuilder.Append(" ");
            }

            var length = LogicalOperator.ToString().Length + 2;
            stringBuilder.Remove(stringBuilder.Length - length, length);
            
            stringBuilder.Append(")");
        }

    }

}
