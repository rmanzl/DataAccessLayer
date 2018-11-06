using System.Reflection;

namespace RobinManzl.DataAccessLayer.Internal.Model
{

    internal class ColumnModel
    {

        public string ColumnName { get; set; }

        public PropertyInfo Property { get; set; }

    }

}
