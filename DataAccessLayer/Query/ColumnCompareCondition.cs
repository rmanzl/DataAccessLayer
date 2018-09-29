using System.Collections.Generic;
using System.Text;

namespace EntityDb.DataAccessLayer.Query
{

    public class ColumnCompareCondition : QueryCondition
    {
        public override ConditionType ConditionType => ConditionType.ValueCompare;

        public string FirstTableName { get; set; }

        public string FirstAttributeName { get; set; }

        public string SecondTableName { get; set; }

        public string SecondAttributeName { get; set; }

        public Operator Operator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            if (FirstTableName == null)
            {
                FirstTableName = tableName;
            }
            if (SecondTableName == null)
            {
                SecondTableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(FirstTableName);
            stringBuilder.Append("].[");
            stringBuilder.Append(FirstAttributeName);
            stringBuilder.Append("] ");

            stringBuilder.Append(Operator.ConvertToSql());

            stringBuilder.Append("[");
            stringBuilder.Append(SecondAttributeName);
            stringBuilder.Append("].[");
            stringBuilder.Append(SecondAttributeName);
            stringBuilder.Append("]");
        }

    }

}
