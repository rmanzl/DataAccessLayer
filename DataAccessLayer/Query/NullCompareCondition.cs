using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Query
{

    public class NullCompareCondition : QueryCondition
    {

        public override ConditionType ConditionType => ConditionType.NullCompare;

        public string TableName { get; set; }

        public string AttributeName { get; set; }

        public bool ShouldBeNull { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameter, string tableName)
        {
            if (TableName == null)
            {
                TableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] IS");

            if (!ShouldBeNull)
            {
                stringBuilder.Append(" NOT");
            }
            stringBuilder.Append(" NULL");
        }

    }

}
