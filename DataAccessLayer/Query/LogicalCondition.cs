using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine logische Verkettung zweier Abfrageknoten dar
    /// </summary>
    public sealed class LogicalCondition : QueryCondition
    {

        /// <inheritdoc />
        public override ConditionType ConditionType => ConditionType.Concatenation;

        /// <summary>
        /// Der linke Knoten der Verkettung
        /// </summary>
        public QueryCondition LeftCondition { get; set; }

        /// <summary>
        /// Der rechte Knoten der Verkettung
        /// </summary>
        public QueryCondition RightCondition { get; set; }

        /// <summary>
        /// Der logische Operator, welcher für die Verkettung verwendet werden soll
        /// </summary>
        public LogicalOperator LogicalOperator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            stringBuilder.Append("(");
            LeftCondition.GenerateConditionString(stringBuilder, parameters, tableName);

            stringBuilder.Append(" ");
            stringBuilder.Append(LogicalOperator.ToString().ToUpper());
            stringBuilder.Append(" ");

            RightCondition.GenerateConditionString(stringBuilder, parameters, tableName);
            stringBuilder.Append(")");
        }

    }

}
