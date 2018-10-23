using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Exceptions;
using RobinManzl.DataAccessLayer.Internal.Model;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class ModelBuilder<T>
        where T : new()
    {

        private List<PropertyInfo> GetProperties()
        {
            var properties = typeof(T).GetRuntimeProperties();

            return properties.Where(prop => prop.GetCustomAttribute<ColumnAttribute>() != null)
                .ToList();
        }

        public EntityModel BuildEntityModel()
        {
            var model = new EntityModel();

            TableBaseAttribute attribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (attribute == null)
            {
                attribute = typeof(T).GetCustomAttribute<ViewAttribute>();
            }

            model.HasIdentityColumn = attribute?.HasIdentityColumn ?? true;

            if (attribute is ViewAttribute viewAttribute)
            {
                model.IsView = true;
                model.ProcedureSchema = viewAttribute.ProcedureSchema;
                model.InsertProcedure = viewAttribute.InsertProcedure;
                model.UpdateProcedure = viewAttribute.UpdateProcedure;
                model.DeleteProcedure = viewAttribute.DeleteProcedure;
            }

            var properties = GetProperties();
            
            foreach (var property in properties)
            {
                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    model.PrimaryKeyProperty = property;
                    model.PrimaryKeyName = property.Name;
                }
            }

            if (model.PrimaryKeyProperty == null)
            {
                throw new InvalidEntityClassException();
            }

            return model;
        }

    }

}
