using System;
using System.Collections.Generic;
using System.Text;

namespace EntityDb.DataAccessLayer.Query
{

    public class ValueCompareCondition : QueryCondition
    {

        public override ConditionType ConditionType => ConditionType.ValueCompare;

        public string TableName { get; set; }

        public string AttributeName { get; set; }

        public object Value { get; set; }

        public Operator Operator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            if (TableName == null)
            {
                TableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] ");

            stringBuilder.Append(Operator.ConvertToSql());
            stringBuilder.Append(" @");

            var attributeName = AttributeName + Guid.NewGuid()
                                                    .ToString()
                                                    .Replace("-", string.Empty);
            stringBuilder.Append(attributeName);

            parameters.Add(attributeName, Value);
        }

    }

}
