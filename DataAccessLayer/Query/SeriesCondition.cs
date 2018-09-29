using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Query
{

    public class SeriesCondition : QueryCondition
    {

        public override ConditionType ConditionType => ConditionType.Conjunktion;

        public List<QueryCondition> Conditions { get; set; }

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
