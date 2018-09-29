using System;

namespace DataAccessLayer
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {

        public string Schema;

        public string Name;

        public TableAttribute(string name = null, string schema = null)
        {
            Name = name;
            Schema = schema;
        }

    }

}
