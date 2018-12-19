using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Exceptions;
using RobinManzl.DataAccessLayer.Internal.Model;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class ModelBuilder
    {

        private static readonly Dictionary<Type, Tuple<EntityModel, ScriptGenerator>> ModelCache;

        static ModelBuilder()
        {
            ModelCache = new Dictionary<Type, Tuple<EntityModel, ScriptGenerator>>();
        }

        private readonly ILogger _logger;

        private readonly bool _useNLog;

        private readonly Type _type;

        public EntityModel EntityModel
        {
            get
            {
                if (!ModelCache.TryGetValue(_type, out var model))
                {
                    _logger?.Info($"Building EntityModel for type {_type.FullName}");

                    var entityModel = BuildEntityModel();
                    model = Tuple.Create(entityModel, new ScriptGenerator(entityModel, _logger, _useNLog));
                    ModelCache[_type] = model;
                }
                else
                {
                    _logger?.Info($"EntityModel for type {_type.FullName} already cached");
                }

                return model.Item1;
            }
        }

        public ScriptGenerator ScriptGenerator
        {
            get
            {
                if (!ModelCache.TryGetValue(_type, out var model))
                {
                    throw new Exception($"ScriptGenerator for type {_type.FullName} missing");
                }

                return model.Item2;
            }
        }

        public ModelBuilder(Type type, ILogger logger, bool useNLog)
        {
            _logger = logger ?? (useNLog ? new NLogWrapper(LogManager.GetCurrentClassLogger()) : null);
            _useNLog = useNLog;

            _type = type;
        }

        private EntityModel BuildEntityModel()
        {
            // TODO: add logging

            var model = new EntityModel();

            TableBaseAttribute attribute = _type.GetCustomAttribute<TableAttribute>();
            if (attribute == null)
            {
                attribute = _type.GetCustomAttribute<ViewAttribute>();
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

            if (attribute != null)
            {
                model.TableName = string.Empty;
                if (attribute.Schema != null)
                {
                    model.TableName += attribute.Schema + "].[";
                }

                if (attribute.Name != null)
                {
                    model.TableName += attribute.Name;
                }
                else
                {
                    model.TableName += _type.Name;
                }
            }
            else
            {
                model.TableName = _type.Name;
            }

            var properties = GetProperties();
            model.Columns = new List<ColumnModel>();
            
            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();

                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    model.PrimaryKeyProperty = property;

                    if (columnAttribute?.Name != null)
                    {
                        model.PrimaryKeyName = columnAttribute.Name;
                    }
                    else
                    {
                        model.PrimaryKeyName = property.Name;
                    }
                }
                else
                {
                    var columnName = property.Name;

                    if (columnAttribute?.Name != null)
                    {
                        columnName = columnAttribute.Name;
                    }

                    model.Columns.Add(new ColumnModel()
                    {
                        ColumnName = columnName,
                        Property = property
                    });
                }
            }

            if (model.PrimaryKeyProperty == null)
            {
                throw new InvalidEntityClassException("Specified entity class is not in the expected format. Make sure the class contains one property marked with a PrimaryKeyAttribute.");
            }

            return model;
        }

        private List<PropertyInfo> GetProperties()
        {
            var properties = _type.GetRuntimeProperties();

            return properties.Where(prop => prop.GetCustomAttribute<ColumnAttribute>() != null)
                .ToList();
        }

    }

}
