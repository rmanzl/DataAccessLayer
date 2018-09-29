using System;

namespace EntityDb.DataAccessLayer
{
    
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {

        public string Name;

        public ColumnAttribute()
        {
        }

        public ColumnAttribute(string name)
        {
            Name = name;
        }

    }

}
