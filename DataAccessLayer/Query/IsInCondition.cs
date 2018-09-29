using System;
using System.Collections.Generic;
using System.Text;

namespace EntityDb.DataAccessLayer.Query
{

    public class IsInCondition : QueryCondition
    {
        public override ConditionType ConditionType => ConditionType.ValueCompare;

        public string TableName { get; set; }

        public string AttributeName { get; set; }

        public List<object> Values { get; set; }

        public bool ShouldBeIn { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            if (Values.Count == 0)
            {
                stringBuilder.Append("1 = 0");
                return;
            }

            if (TableName == null)
            {
                TableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].");

            stringBuilder.Append("[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] ");

            if (!ShouldBeIn)
            {
                stringBuilder.Append("NOT ");
            }

            stringBuilder.Append("IN (@");

            for(int i = 0; i < Values.Count; i++)
            {
                var attributeName = AttributeName + Guid.NewGuid()
                                                    .ToString()
                                                    .Replace("-", string.Empty);

                stringBuilder.Append(attributeName);
                parameters.Add(attributeName, Values[i]);

                if(i != Values.Count - 1)
                {
                    stringBuilder.Append(", @");
                }
            }

            stringBuilder.Append(")");
        }

    }

}
