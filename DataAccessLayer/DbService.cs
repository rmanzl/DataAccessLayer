using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using DataAccessLayer.Query;

namespace DataAccessLayer
{
    
    public class DbService<T>
        where T : IEntity, new()
    {

        private readonly object _lock;

        private readonly SqlConnection _connection;

        private readonly bool _isView;

        private readonly ScriptGenerator<T> _scriptGenerator;

        private readonly EntityParser<T> _entityParser;

        public string LastErrorMessage { get; private set; }
        
        public DbService(SqlConnection connection, bool isView = false)
        {
            _lock = new object();

            _connection = connection;

            _isView = isView;

            List<PropertyInfo> properties = GetProperties();
            _scriptGenerator = new ScriptGenerator<T>(properties);
            _entityParser = new EntityParser<T>(properties);
        }

        private static List<PropertyInfo> GetProperties()
        {
            var properties = typeof(T).GetRuntimeProperties();

            return properties.Where(prop => prop.GetCustomAttribute<ColumnAttribute>() != null)
                             .ToList();
        }

        public T GetEntityById(int id)
        {
            List<T> entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = nameof(IEntity.Id),
                Value = id,
                Operator = Operator.Equals
            });

            return entities.Single();
        }

        public List<T> GetEntities(QueryCondition queryCondition = null, string joinStatement = null, QueryOptions options = null)
        {
            lock (_lock)
            {
                _connection.Open();
                try
                {
                    string query;
                    var parameters = new Dictionary<string, object>();

                    if (queryCondition != null ||
                        joinStatement != null)
                    {
                        query = _scriptGenerator.GetSelectQuery(parameters, queryCondition, joinStatement, options);
                    }
                    else
                    {
                        query = _scriptGenerator.GetSelectQuery(null, null, null, options);
                    }

                    var command = new SqlCommand(query, _connection);
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    //_logger?.Debug(GenerateLoggingMessage(command));

                    SqlDataReader reader = command.ExecuteReader();

                    var entities = new List<T>();
                    while (reader.Read())
                    {
                        entities.Add(_entityParser.ParseEntity(reader));
                    }

                    return entities;
                }
                catch (Exception exception)
                {
                    //_logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;
                    throw;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        public List<T> GetTopNEntities(int count, QueryCondition queryCondition = null, string joinStatement = null)
        {
            return GetEntities(queryCondition, joinStatement, new QueryOptions()
            {
                MaxRowCount = count
            });
        }

        public T GetFirstEntity(QueryCondition queryCondition = null, string joinStatement = null)
        {
            return GetEntities(queryCondition, joinStatement, new QueryOptions()
            {
                MaxRowCount = 1
            }).First();
        }

        public T GetFirstOrDefaultEntity(QueryCondition queryCondition = null, string joinStatement = null)
        {
            return GetEntities(queryCondition, joinStatement, new QueryOptions()
            {
                MaxRowCount = 1
            }).FirstOrDefault();
        }

        public bool InsertEntity(T entity)
        {
            if (_isView)
            {
                LastErrorMessage = "Cannot insert entity into a view";
                return false;
            }

            lock (_lock)
            {
                try
                {
                    _connection.Open();

                    var command = new SqlCommand(_scriptGenerator.GetInsertQuery(), _connection);
                    AssignParameters(entity, command);

                    //_logger?.Debug(GenerateLoggingMessage(command));

                    var result = (int)command.ExecuteScalar();
                    entity.Id = result;

                    return true;
                }
                catch (Exception exception)
                {
                    //_logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;
                    return false;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        public bool UpdateEntity(T entity)
        {
            if (_isView)
            {
                LastErrorMessage = "Cannot update entity of a view";
                return false;
            }

            lock (_lock)
            {
                try
                {
                    _connection.Open();

                    var command = new SqlCommand(_scriptGenerator.GetUpdateQuery(), _connection);
                    AssignParameters(entity, command);

                    //_logger?.Debug(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    //_logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;
                    return false;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        private void AssignParameters(T entity, SqlCommand command)
        {
            foreach (KeyValuePair<string, object> parameter in _entityParser.GetParameters(entity))
            {
                var sqlParameter = new SqlParameter()
                {
                    ParameterName = parameter.Key,
                    Value = parameter.Value
                };

                // Workaround für Image-datentyp
                /*if (parameter.Key.Equals("Image"))
                {
                    sqlParameter.DbType = DbType.Binary;
                    sqlParameter.SqlDbType = System.Data.SqlDbType.Image;
                }*/

                command.Parameters.Add(sqlParameter);
            }
        }

        public bool DeleteEntity(int entityId)
        {
            if (_isView)
            {
                LastErrorMessage = "Cannot delete entity of a view";
                return false;
            }

            lock (_lock)
            {
                try
                {
                    _connection.Open();

                    var command = new SqlCommand(_scriptGenerator.GetDeleteQuery(), _connection);
                    command.Parameters.AddWithValue(nameof(IEntity.Id), entityId);

                    //_logger?.Debug(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    //_logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;
                    return false;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        /// <summary>
        /// Formatiert die Logging-Message aus einem SqlCommand-Objekt
        /// </summary>
        private string GenerateLoggingMessage(SqlCommand command)
        {
            var message = "Execute statement: {";

            message += command.CommandText;
            message += "} - {@";

            message += string.Join(", @", command.Parameters.Cast<SqlParameter>().Select(par => par.ParameterName + " = '" + par.Value + "'"));

            message += "}";

            return message;
        }

    }

}
