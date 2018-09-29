using System.Collections.Generic;
using System.Text;

namespace EntityDb.DataAccessLayer.Query
{

    public abstract class QueryCondition
    {
        
        public abstract ConditionType ConditionType { get; }

        internal abstract void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName);

    }

}
