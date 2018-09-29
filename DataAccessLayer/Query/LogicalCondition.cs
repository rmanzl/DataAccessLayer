using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Query
{

    public class LogicalCondition : QueryCondition
    {

        public override ConditionType ConditionType => ConditionType.Conjunktion;

        public QueryCondition LeftCondition { get; set; }

        public QueryCondition RightCondition { get; set; }

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
