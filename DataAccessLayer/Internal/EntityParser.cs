using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace RobinManzl.DataAccessLayer.Internal
{
    
    internal class EntityParser<T>
        where T : IEntity, new()
    {

        private readonly List<PropertyInfo> _properties;

        public EntityParser(List<PropertyInfo> properties)
        {
            _properties = properties;
        }
        
        public T ParseEntity(SqlDataReader reader)
        {
            var entity = new T();

            for (int i = 0; i < _properties.Count; i++)
            {
                var value = reader.GetValue(i);
                if (value == DBNull.Value)
                {
                    value = null;
                }
                
                _properties[i].SetValue(entity, value);
            }

            return entity;
        }
        
        public Dictionary<string, object> GetParameters(T entity)
        {
            var parameters = new Dictionary<string, object>();

            foreach (var property in _properties)
            {
                parameters[property.Name] = property.GetValue(entity) ?? DBNull.Value;
            }

            return parameters;
        }

    }

}
