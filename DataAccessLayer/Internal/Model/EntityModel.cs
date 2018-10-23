using System.Collections.Generic;
using System.Reflection;

namespace RobinManzl.DataAccessLayer.Internal.Model
{

    internal class EntityModel
    {

        public string TableName { get; set; }

        public bool IsView { get; set; }

        public bool HasIdentityColumn { get; set; }

        public string ProcedureSchema { get; set; }

        public string InsertProcedure { get; set; }

        public string UpdateProcedure { get; set; }

        public string DeleteProcedure { get; set; }

        public string PrimaryKeyName { get; set; }

        public PropertyInfo PrimaryKeyProperty { get; set; }

        public List<ColumnModel> Columns { get; set; }

    }

}
