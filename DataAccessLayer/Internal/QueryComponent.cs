using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using RobinManzl.DataAccessLayer.Internal.Model;
using RobinManzl.DataAccessLayer.Query;
using RobinManzl.DataAccessLayer.Query.Conditions;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class QueryComponent<T>
        where T : new()
    {

        private readonly DbService<T> _dbService;

        private readonly EntityModel _entityModel;

        public QueryComponent(DbService<T> dbService, EntityModel entityModel)
        {
            _dbService = dbService;
            _entityModel = entityModel;
        }

        public List<T> GetEntities(QueryCondition queryCondition = null, QueryOptions options = null)
        {
            lock (_dbService.Lock)
            {
                _dbService.Logger?.Debug(nameof(GetEntities));

                var opened = _dbService.Connection.State != ConnectionState.Open;

                if (opened)
                {
                    _dbService.Logger?.Info("Opening connection");
                    _dbService.Connection.Open();
                }

                try
                {
                    string query;
                    var parameters = new Dictionary<string, object>();

                    if (queryCondition != null)
                    {
                        query = _dbService.ScriptGenerator.GetSelectQuery(parameters, queryCondition, options);
                    }
                    else
                    {
                        query = _dbService.ScriptGenerator.GetSelectQuery(null, null, options);
                    }

                    var command = new SqlCommand(query, _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    var reader = command.ExecuteReader();

                    var entities = new List<T>();
                    while (reader.Read())
                    {
                        entities.Add(_dbService.EntityParser.ParseEntity(reader));
                    }

                    reader.Close();
                    reader.Dispose();

                    _dbService.Logger?.Info($"Returned {entities.Count} rows from database");

                    return entities;
                }
                catch (Exception exception)
                {
                    _dbService.Logger?.Error("Error while executing query", exception);
                    _dbService.LastErrorMessage = exception.Message;

                    if (_dbService.CurrentTransaction != null)
                    {
                        _dbService.Logger?.Info("Rollback transaction and closing connection");
                        _dbService.CurrentTransaction.Rollback();
                        _dbService.CurrentTransaction = null;
                        _dbService.Connection.Close();
                    }

                    throw;
                }
                finally
                {
                    if (opened)
                    {
                        _dbService.Logger?.Info("Closing connection");
                        _dbService.Connection.Close();
                    }
                }
            }
        }

        public List<T> GetEntities(Expression<Func<T, bool>> expression, QueryOptions options = null)
        {
            return GetEntities(ExpressionConverter.ToQueryCondition(expression, typeof(T)), options);
        }
        
        public List<T> GetEntities(QueryOptions options)
        {
            return GetEntities((QueryCondition)null, options);
        }
        
        public T GetEntityById(int id)
        {
            _dbService.Logger?.Debug(nameof(GetEntityById));

            var entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = _entityModel.PrimaryKeyName,
                Value = id,
                Operator = Operator.Equals
            });

            return entities.Single();
        }
        
        public T TryGetEntityById(int id)
        {
            _dbService.Logger?.Debug(nameof(GetEntityById));

            var entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = _entityModel.PrimaryKeyName,
                Value = id,
                Operator = Operator.Equals
            });

            return entities.SingleOrDefault();
        }
        
        public List<T> GetTopNEntities(int count, QueryCondition queryCondition = null)
        {
            _dbService.Logger?.Debug(nameof(GetTopNEntities));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = count
            });
        }
        
        public List<T> GetTopNEntities(int count, Expression<Func<T, bool>> expression)
        {
            _dbService.Logger?.Debug(nameof(GetTopNEntities));

            return GetTopNEntities(count, ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }
        
        public T GetFirstEntity(QueryCondition queryCondition = null)
        {
            _dbService.Logger?.Debug(nameof(GetFirstEntity));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = 1
            }).First();
        }
        
        public T GetFirstEntity(Expression<Func<T, bool>> expression)
        {
            _dbService.Logger?.Debug(nameof(GetFirstEntity));

            return GetFirstEntity(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }
        
        public T GetFirstOrDefaultEntity(QueryCondition queryCondition = null)
        {
            _dbService.Logger?.Debug(nameof(GetFirstOrDefaultEntity));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = 1
            }).FirstOrDefault();
        }

        public T GetFirstOrDefaultEntity(Expression<Func<T, bool>> expression)
        {
            return GetFirstOrDefaultEntity(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

        public TValue GetMax<TValue>(string attributeName, QueryCondition queryCondition = null)
        {
            lock (_dbService.Lock)
            {
                _dbService.Logger?.Debug(nameof(GetMax));

                var opened = _dbService.Connection.State != ConnectionState.Open;

                if (opened)
                {
                    _dbService.Logger?.Info("Opening connection");
                    _dbService.Connection.Open();
                }

                try
                {
                    string query;
                    var parameters = new Dictionary<string, object>();

                    if (queryCondition != null)
                    {
                        query = _dbService.ScriptGenerator.GetSelectMaxQuery(attributeName, parameters, queryCondition);
                    }
                    else
                    {
                        query = _dbService.ScriptGenerator.GetSelectMaxQuery(attributeName, parameters);
                    }

                    var command = new SqlCommand(query, _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    var result = command.ExecuteScalar();

                    if (result == DBNull.Value)
                    {
                        result = null;
                    }

                    _dbService.Logger?.Info("Returned value rows from database");

                    return (TValue)result;
                }
                catch (Exception exception)
                {
                    _dbService.Logger?.Error("Error while executing query", exception);
                    _dbService.LastErrorMessage = exception.Message;

                    if (_dbService.CurrentTransaction != null)
                    {
                        _dbService.Logger?.Info("Rollback transaction and closing connection");
                        _dbService.CurrentTransaction.Rollback();
                        _dbService.CurrentTransaction = null;
                        _dbService.Connection.Close();
                    }

                    throw;
                }
                finally
                {
                    if (opened)
                    {
                        _dbService.Logger?.Info("Closing connection");
                        _dbService.Connection.Close();
                    }
                }
            }
        }

        public TValue GetMax<TValue>(string attributeName, Expression<Func<T, bool>> expression = null)
        {
            return GetMax<TValue>(attributeName, ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

    }

}
