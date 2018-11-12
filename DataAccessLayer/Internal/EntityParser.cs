using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using RobinManzl.DataAccessLayer.Internal.Model;

namespace RobinManzl.DataAccessLayer.Internal
{
    
    internal class EntityParser<T>
        where T : new()
    {

        private readonly EntityModel _entityModel;

        public EntityParser(EntityModel entityModel)
        {
            _entityModel = entityModel;
        }
        
        public T ParseEntity(SqlDataReader reader)
        {
            var entity = new T();

            _entityModel.PrimaryKeyProperty.SetValue(entity, reader.GetValue(0));

            for (var i = 0; i < _entityModel.Columns.Count; i++)
            {
                var value = reader.GetValue(i + 1);
                if (value == DBNull.Value)
                {
                    value = null;
                }

                _entityModel.Columns[i].Property.SetValue(entity, value);
            }

            return entity;
        }
        
        public Dictionary<string, object> GetParameters(T entity)
        {
            var parameters = new Dictionary<string, object>()
            {
                [_entityModel.PrimaryKeyProperty.Name] = _entityModel.PrimaryKeyProperty.GetValue(entity) ?? DBNull.Value 
            };

            foreach (var column in _entityModel.Columns)
            {
                parameters[column.Property.Name] = column.Property.GetValue(entity) ?? DBNull.Value;
            }

            return parameters;
        }

    }

}
