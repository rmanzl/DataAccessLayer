using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using RobinManzl.DataAccessLayer.Internal.Model;
using RobinManzl.DataAccessLayer.Query;
// ReSharper disable InconsistentlySynchronizedField

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class DataManipulationComponent<T>
        where T : new()
    {

        private readonly DbService<T> _dbService;

        private readonly EntityModel _entityModel;

        public DataManipulationComponent(DbService<T> dbService, EntityModel entityModel)
        {
            _dbService = dbService;
            _entityModel = entityModel;
        }

        private void AssignParameters(T entity, SqlCommand command)
        {
            foreach (var parameter in _dbService.EntityParser.GetParameters(entity))
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

        public bool InsertEntity(T entity)
        {
            _dbService.Logger?.Debug(nameof(InsertEntity));

            if (_entityModel.IsView &&
                _entityModel.InsertProcedure == null)
            {
                _dbService.Logger?.Warning("Trying to insert row into view without specifying a stored procedure");
                _dbService.LastErrorMessage = "Cannot insert entity into a view";
                return false;
            }

            lock (_dbService.Lock)
            {
                var opened = _dbService.Connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _dbService.Connection.Open();
                    }

                    string commandText;
                    if (_entityModel.IsView)
                    {
                        commandText = $"[{(_entityModel.ProcedureSchema != null ? _entityModel.ProcedureSchema + "].[" : "")}{_entityModel.InsertProcedure}]";
                    }
                    else
                    {
                        commandText = _dbService.ScriptGenerator.GetInsertQuery();
                    }

                    var command = new SqlCommand(commandText, _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    if (_entityModel.IsView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    AssignParameters(entity, command);

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    if (_entityModel.HasIdentityColumn)
                    {
                        var result = command.ExecuteScalar();
                        _entityModel.PrimaryKeyProperty.SetValue(entity, result);
                    }
                    else
                    {
                        command.ExecuteScalar();
                    }

                    return true;
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

                    return false;
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
        
        public bool UpdateEntity(T entity)
        {
            _dbService.Logger?.Debug(nameof(UpdateEntity));

            if (_entityModel.IsView &&
                _entityModel.UpdateProcedure == null)
            {
                _dbService.Logger?.Warning("Trying to update row of view without specifying a stored procedure");
                _dbService.LastErrorMessage = "Cannot update entity of a view";
                return false;
            }

            lock (_dbService.Lock)
            {
                var opened = _dbService.Connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _dbService.Connection.Open();
                    }

                    string commandText;
                    if (_entityModel.IsView)
                    {
                        commandText = $"[{(_entityModel.ProcedureSchema != null ? _entityModel.ProcedureSchema + "].[" : "")}{_entityModel.UpdateProcedure}]";
                    }
                    else
                    {
                        commandText = _dbService.ScriptGenerator.GetUpdateQuery();
                    }

                    var command = new SqlCommand(commandText, _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    if (_entityModel.IsView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    AssignParameters(entity, command);

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
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

                    return false;
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
        
        public bool DeleteEntity(int entityId)
        {
            _dbService.Logger?.Debug(nameof(DeleteEntity));

            if (_entityModel.IsView &&
                _entityModel.DeleteProcedure == null)
            {
                _dbService.Logger?.Warning("Trying to delete row of view without specifying a stored procedure");
                _dbService.LastErrorMessage = "Cannot delete entity of a view";
                return false;
            }

            lock (_dbService.Lock)
            {
                var opened = _dbService.Connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _dbService.Connection.Open();
                    }

                    string commandText;
                    if (_entityModel.IsView)
                    {
                        commandText = $"[{(_entityModel.ProcedureSchema != null ? _entityModel.ProcedureSchema + "].[" : "")}{_entityModel.DeleteProcedure}]";
                    }
                    else
                    {
                        commandText = _dbService.ScriptGenerator.GetDeleteQuery();
                    }

                    var command = new SqlCommand(commandText, _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    if (_entityModel.IsView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    command.Parameters.AddWithValue(nameof(_entityModel.PrimaryKeyProperty.Name), entityId);

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
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

                    return false;
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
        
        public bool DeleteEntities(QueryCondition queryCondition)
        {
            _dbService.Logger?.Debug(nameof(DeleteEntities));

            if (_entityModel.IsView)
            {
                _dbService.Logger?.Warning("Deleting rows of view with a query condition is not supported");
                _dbService.LastErrorMessage = "Cannot delete entities of a view";
                return false;
            }

            lock (_dbService.Lock)
            {
                var opened = _dbService.Connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _dbService.Connection.Open();
                    }

                    var parameters = new Dictionary<string, object>();

                    var command = new SqlCommand(_dbService.ScriptGenerator.GetDeleteQuery(parameters, queryCondition), _dbService.Connection);
                    if (_dbService.CurrentTransaction != null)
                    {
                        command.Transaction = _dbService.CurrentTransaction;
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    _dbService.Logger?.Info(_dbService.GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
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

                    return false;
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
        
        public bool DeleteEntities(Expression<Func<T, bool>> expression)
        {
            _dbService.Logger?.Debug(nameof(DeleteEntities));

            return DeleteEntities(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

    }

}
