using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NLog;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Exceptions;
using RobinManzl.DataAccessLayer.Internal;
using RobinManzl.DataAccessLayer.Query;
using RobinManzl.DataAccessLayer.Query.Conditions;

namespace RobinManzl.DataAccessLayer
{

    /// <summary>
    /// Diese Klasse kann verwendet werden, um Daten aus einer Datenbank auszulesen und zu ändern
    /// </summary>
    /// <typeparam name="T">
    /// Die Klasse gibt an, für welche Tabelle Daten ausgelesen werden sollen
    /// </typeparam>
    public class DbService<T>
        where T : new()
    {

        private readonly object _lock;

        private readonly SqlConnection _connection;

        private readonly ILogger _logger;

        private readonly bool _isView;

        private readonly string _procedureSchema;

        private readonly string _insertProcedure;

        private readonly string _updateProcedure;

        private readonly string _deleteProcedure;

        private readonly PropertyInfo _primaryKeyProperty;

        private readonly ScriptGenerator<T> _scriptGenerator;

        private readonly EntityParser<T> _entityParser;

        private SqlTransaction _currentTransaction;

        /// <summary>
        /// Beinhaltet die zuletzt generierte Fehlermeldung, sofern eine existiert
        /// </summary>
        public string LastErrorMessage { get; private set; }

        /// <summary>
        /// Erstellt eine neue Instanz eines DbServices für eine bestimmte Entity-Klasse
        /// </summary>
        /// <param name="connection">
        /// Die SqlConnection, welche für die Datenbank-Verbindungen verwendet wird
        /// </param>
        /// <param name="logger">
        /// Die Logger-Instanz, welche bei allen Vorgängen verwendet wird
        /// </param>
        public DbService(SqlConnection connection, ILogger logger = null)
        {
            _lock = new object();

            _connection = connection;

            _logger = logger;
            _logger?.Info($"Creating DbService for entity {typeof(T).FullName}");

            var viewAttribute = typeof(T).GetCustomAttribute<ViewAttribute>();
            if (viewAttribute != null)
            {
                _isView = true;
                _procedureSchema = viewAttribute.ProcedureSchema;
                _insertProcedure = viewAttribute.InsertProcedure;
                _updateProcedure = viewAttribute.UpdateProcedure;
                _deleteProcedure = viewAttribute.DeleteProcedure;
            }

            var properties = GetProperties();

            var primaryKeyName = "Id";
            foreach (var property in properties)
            {
                _logger?.Debug($"Entity {typeof(T).Name} contains property '{property.Name}' mapping db field [{property.GetCustomAttribute<ColumnAttribute>().Name ?? property.Name}]");

                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    _primaryKeyProperty = property;
                    primaryKeyName = property.Name;
                }
            }

            if (_primaryKeyProperty == null)
            {
                throw new InvalidEntityClassException();
            }

            _logger?.Debug($"Primary key property of entity {typeof(T).Name}: {_primaryKeyProperty.Name}");

            _scriptGenerator = new ScriptGenerator<T>(properties, primaryKeyName);
            _entityParser = new EntityParser<T>(properties);
        }

        /// <summary>
        /// Erstellt eine neue Instanz eines DbServices für eine bestimmte Entity-Klasse
        /// </summary>
        /// <param name="connection">
        /// Die SqlConnection, welche für die Datenbank-Verbindungen verwendet wird
        /// </param>
        /// <param name="logger">
        /// Die NLog-Logger-Instanz, welche bei allen Vorgängen verwendet wird
        /// </param>
        public DbService(SqlConnection connection, Logger logger)
            : this(connection, new NLogWrapper(logger))
        {
        }

        /// <summary>
        /// Erstellt eine neue Instanz eines DbServices für eine bestimmte Entity-Klasse
        /// </summary>
        /// <param name="connection">
        /// Die SqlConnection, welche für die Datenbank-Verbindungen verwendet wird
        /// </param>
        /// <param name="useNLog">
        /// Gibt an, ob eine NLog-Instanz erstellt und verwendet werden soll
        /// </param>
        public DbService(SqlConnection connection, bool useNLog)
            : this(connection, useNLog ? new NLogWrapper(LogManager.GetCurrentClassLogger()) : null)
        {
        }

        private static List<PropertyInfo> GetProperties()
        {
            var properties = typeof(T).GetRuntimeProperties();

            return properties.Where(prop => prop.GetCustomAttribute<ColumnAttribute>() != null)
                             .ToList();
        }

        /// <summary>
        /// Startet eine neue Transaktion oder übernimmt die angegebene Transaktion
        /// </summary>
        /// <param name="transaction">
        /// Falls bereits eine Transaktion gestartet wurde, kann sie durch diesen Parameter übergeben werden
        /// </param>
        /// <returns>
        /// Gibt die Transaktion zurück
        /// </returns>
        public SqlTransaction BeginTransaction(SqlTransaction transaction = null)
        {
            _logger?.Debug(nameof(BeginTransaction));

            lock (_lock)
            {
                _logger?.Info("Beginning transaction");

                if (_connection.State != ConnectionState.Open)
                {
                    _logger?.Info("Opening connection");
                    _connection.Open();
                }

                _currentTransaction = transaction ?? _connection.BeginTransaction();

                return _currentTransaction;
            }
        }

        /// <summary>
        /// Entfernt die aktuelle Transaktion aus diesem DbService
        /// </summary>
        public void RemoveTransaction()
        {
            _logger?.Debug(nameof(RemoveTransaction));

            lock (_lock)
            {
                _logger?.Info("Removing transaction");

                _currentTransaction = null;

                if (_connection.State == ConnectionState.Open)
                {
                    _logger?.Info("Closing connection");
                    _connection.Close();
                }
            }
        }

        /// <summary>
        /// Führt eine Abfrage gegen die Tabelle aus
        /// </summary>
        /// <param name="queryCondition">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <param name="options">
        /// Kann verwendet werden, um Optionen für die Abfrage anzugeben
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public List<T> GetEntities(QueryCondition queryCondition = null, QueryOptions options = null)
        {
            _logger?.Debug(nameof(GetEntities));

            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                if (opened)
                {
                    _logger?.Info("Opening connection");
                    _connection.Open();
                }

                try
                {
                    string query;
                    var parameters = new Dictionary<string, object>();

                    if (queryCondition != null)
                    {
                        query = _scriptGenerator.GetSelectQuery(parameters, queryCondition, options);
                    }
                    else
                    {
                        query = _scriptGenerator.GetSelectQuery(null, null, options);
                    }

                    var command = new SqlCommand(query, _connection);
                    if (_currentTransaction != null)
                    {
                        command.Transaction = _currentTransaction;
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    _logger?.Info(GenerateLoggingMessage(command));

                    var reader = command.ExecuteReader();

                    var entities = new List<T>();
                    while (reader.Read())
                    {
                        entities.Add(_entityParser.ParseEntity(reader));
                    }

                    reader.Close();
                    reader.Dispose();

                    _logger?.Info($"Returned {entities.Count} rows from database");

                    return entities;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
                        _logger?.Info("Rollback transaction and closing connection");
                        _currentTransaction.Rollback();
                        _currentTransaction = null;
                        _connection.Close();
                    }

                    throw;
                }
                finally
                {
                    if (opened)
                    {
                        _logger?.Info("Closing connection");
                        _connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Führt eine Abfrage gegen die Tabelle aus
        /// </summary>
        /// <param name="expression">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <param name="options">
        /// Kann verwendet werden, um Optionen für die Abfrage anzugeben
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public List<T> GetEntities(Expression<Func<T, bool>> expression, QueryOptions options = null)
        {
            _logger?.Debug(nameof(GetEntities));

            return GetEntities(ExpressionConverter.ToQueryCondition(expression, typeof(T)), options);
        }

        /// <summary>
        /// Führt eine Abfrage gegen die Tabelle aus
        /// </summary>
        /// <param name="options">
        /// Kann verwendet werden, um Optionen für die Abfrage anzugeben
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public List<T> GetEntities(QueryOptions options)
        {
            _logger?.Debug(nameof(GetEntities));

            return GetEntities((QueryCondition)null, options);
        }

        /// <summary>
        /// Frägt eine Zeile der Tabelle anhand ihres Primärschlüssels ab
        /// </summary>
        /// <param name="id">
        /// Der Wert des Primärschlüsselss
        /// </param>
        /// <returns>
        /// Gibt die gefundene Zeile als Objekt zurück
        /// </returns>
        public T GetEntityById(int id)
        {
            _logger?.Debug(nameof(GetEntityById));

            var entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = _primaryKeyProperty.Name,
                Value = id,
                Operator = Operator.Equals
            });

            return entities.Single();
        }

        /// <summary>
        /// Frägt eine Zeile der Tabelle anhand ihres Primärschlüssels ab, falls diese existiert
        /// </summary>
        /// <param name="id">
        /// Der Wert des Primärschlüsselss
        /// </param>
        /// <returns>
        /// Gibt die gefundene Zeile als Objekt oder NULL zurück
        /// </returns>
        public T TryGetEntityById(int id)
        {
            _logger?.Debug(nameof(GetEntityById));

            var entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = _primaryKeyProperty.Name,
                Value = id,
                Operator = Operator.Equals
            });

            return entities.SingleOrDefault();
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die ersten <code>count</code> Zeilen zurück
        /// </summary>
        /// <param name="count">
        /// Gibt an, wieviele Zeilen maximal vom Datenbankserver abgefragt werden sollen
        /// </param>
        /// <param name="queryCondition">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public List<T> GetTopNEntities(int count, QueryCondition queryCondition = null)
        {
            _logger?.Debug(nameof(GetTopNEntities));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = count
            });
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die ersten <code>count</code> Zeilen zurück
        /// </summary>
        /// <param name="count">
        /// Gibt an, wieviele Zeilen maximal vom Datenbankserver abgefragt werden sollen
        /// </param>
        /// <param name="expression">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public List<T> GetTopNEntities(int count, Expression<Func<T, bool>> expression)
        {
            _logger?.Debug(nameof(GetTopNEntities));

            return GetTopNEntities(count, ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die erste Zeile zurück
        /// </summary>
        /// <param name="queryCondition">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public T GetFirstEntity(QueryCondition queryCondition = null)
        {
            _logger?.Debug(nameof(GetFirstEntity));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = 1
            }).First();
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die erste Zeile zurück
        /// </summary>
        /// <param name="expression">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public T GetFirstEntity(Expression<Func<T, bool>> expression)
        {
            _logger?.Debug(nameof(GetFirstEntity));

            return GetFirstEntity(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die erste Zeile zurück
        /// </summary>
        /// <param name="queryCondition">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public T GetFirstOrDefaultEntity(QueryCondition queryCondition = null)
        {
            _logger?.Debug(nameof(GetFirstOrDefaultEntity));

            return GetEntities(queryCondition, new QueryOptions()
            {
                MaxRowCount = 1
            }).FirstOrDefault();
        }

        /// <summary>
        /// Diese Methode kapselt die <code>GetEntities</code>-Methode und gibt nur die erste Zeile zurück
        /// </summary>
        /// <param name="expression">
        /// Spezifiziert die WHERE-Klausel der Abfrage
        /// </param>
        /// <returns>
        /// Gibt eine Liste an Objekten zurück, welche vom Datenbankserver zurückgegeben wurden
        /// </returns>
        public T GetFirstOrDefaultEntity(Expression<Func<T, bool>> expression)
        {
            _logger?.Debug(nameof(GetFirstOrDefaultEntity));

            return GetFirstOrDefaultEntity(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

        /// <summary>
        /// Diese Methode kann verwendet werden, um eine Zeile in der Datenbank anzulegen
        /// </summary>
        /// <param name="entity">
        /// Das anzulegende Objekt
        /// </param>
        /// <returns>
        /// Gibt an, ob der Vorgang erfolgreich war
        /// </returns>
        public bool InsertEntity(T entity)
        {
            _logger?.Debug(nameof(InsertEntity));

            if (_isView &&
                _insertProcedure == null)
            {
                _logger?.Warning("Trying to insert row into view without specifying a stored procedure");
                LastErrorMessage = "Cannot insert entity into a view";
                return false;
            }

            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _connection.Open();
                    }

                    string commandText;
                    if (_isView)
                    {
                        commandText = $"[{(_procedureSchema != null ? _procedureSchema + "].[" : "")}{_insertProcedure}]";
                    }
                    else
                    {
                        commandText = _scriptGenerator.GetInsertQuery();
                    }

                    var command = new SqlCommand(commandText, _connection);
                    if (_currentTransaction != null)
                    {
                        command.Transaction = _currentTransaction;
                    }

                    if (_isView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    AssignParameters(entity, command);

                    _logger?.Info(GenerateLoggingMessage(command));

                    var result = command.ExecuteScalar();
                    _primaryKeyProperty.SetValue(entity, result);

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
                        _logger?.Info("Rollback transaction and closing connection");
                        _currentTransaction.Rollback();
                        _currentTransaction = null;
                        _connection.Close();
                    }

                    return false;
                }
                finally
                {
                    if (opened)
                    {
                        _logger?.Info("Closing connection");
                        _connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Diese Methode kann verwendet werden, um eine Zeile in der Datenbank anzupassen
        /// </summary>
        /// <param name="entity">
        /// Das anzupassende Objekt
        /// </param>
        /// <returns>
        /// Gibt an, ob der Vorgang erfolgreich war
        /// </returns>
        public bool UpdateEntity(T entity)
        {
            _logger?.Debug(nameof(UpdateEntity));

            if (_isView &&
                _updateProcedure == null)
            {
                _logger?.Warning("Trying to update row of view without specifying a stored procedure");
                LastErrorMessage = "Cannot update entity of a view";
                return false;
            }

            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _connection.Open();
                    }

                    string commandText;
                    if (_isView)
                    {
                        commandText = $"[{(_procedureSchema != null ? _procedureSchema + "].[" : "")}{_updateProcedure}]";
                    }
                    else
                    {
                        commandText = _scriptGenerator.GetUpdateQuery();
                    }

                    var command = new SqlCommand(commandText, _connection);
                    if (_currentTransaction != null)
                    {
                        command.Transaction = _currentTransaction;
                    }

                    if (_isView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    AssignParameters(entity, command);

                    _logger?.Info(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
                        _logger?.Info("Rollback transaction and closing connection");
                        _currentTransaction.Rollback();
                        _currentTransaction = null;
                        _connection.Close();
                    }

                    return false;
                }
                finally
                {
                    if (opened)
                    {
                        _logger?.Info("Closing connection");
                        _connection.Close();
                    }
                }
            }
        }

        private void AssignParameters(T entity, SqlCommand command)
        {
            foreach (var parameter in _entityParser.GetParameters(entity))
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

        /// <summary>
        /// Diese Methode kann verwendet werden, um eine Zeile in der Datenbank zu löschen
        /// </summary>
        /// <param name="entityId">
        /// Der Wert des Primärschlüssels des zu löschenden Objektes
        /// </param>
        /// <returns>
        /// Gibt an, ob der Vorgang erfolgreich war
        /// </returns>
        public bool DeleteEntity(int entityId)
        {
            _logger?.Debug(nameof(DeleteEntity));

            if (_isView &&
                _deleteProcedure == null)
            {
                _logger?.Warning("Trying to delete row of view without specifying a stored procedure");
                LastErrorMessage = "Cannot delete entity of a view";
                return false;
            }

            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _connection.Open();
                    }

                    string commandText;
                    if (_isView)
                    {
                        commandText = $"[{(_procedureSchema != null ? _procedureSchema + "].[" : "")}{_deleteProcedure}]";
                    }
                    else
                    {
                        commandText = _scriptGenerator.GetDeleteQuery();
                    }

                    var command = new SqlCommand(commandText, _connection);
                    if (_currentTransaction != null)
                    {
                        command.Transaction = _currentTransaction;
                    }

                    if (_isView)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    command.Parameters.AddWithValue(nameof(_primaryKeyProperty.Name), entityId);

                    _logger?.Info(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
                        _logger?.Info("Rollback transaction and closing connection");
                        _currentTransaction.Rollback();
                        _currentTransaction = null;
                        _connection.Close();
                    }

                    return false;
                }
                finally
                {
                    if (opened)
                    {
                        _logger?.Info("Closing connection");
                        _connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Diese Methode kann verwendet werden, um Zeilen anhang einer QueryCondition in der Datenbank zu löschen
        /// </summary>
        /// <param name="queryCondition">
        /// Die QueryConition, die für die WHERE-Klausel verwendet werden soll
        /// </param>
        /// <returns>
        /// Gibt an, ob der Vorgang erfolgreich war
        /// </returns>
        public bool DeleteEntities(QueryCondition queryCondition)
        {
            _logger?.Debug(nameof(DeleteEntities));

            if (_isView)
            {
                _logger?.Warning("Deleting rows of view with a query condition is not supported");
                LastErrorMessage = "Cannot delete entities of a view";
                return false;
            }

            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                try
                {
                    if (opened)
                    {
                        _connection.Open();
                    }

                    var parameters = new Dictionary<string, object>();

                    var command = new SqlCommand(_scriptGenerator.GetDeleteQuery(parameters, queryCondition), _connection);
                    if (_currentTransaction != null)
                    {
                        command.Transaction = _currentTransaction;
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    _logger?.Info(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
                        _logger?.Info("Rollback transaction and closing connection");
                        _currentTransaction.Rollback();
                        _currentTransaction = null;
                        _connection.Close();
                    }

                    return false;
                }
                finally
                {
                    if (opened)
                    {
                        _logger?.Info("Closing connection");
                        _connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Diese Methode kann verwendet werden, um Zeilen anhang einer Expression in der Datenbank zu löschen
        /// </summary>
        /// <param name="expression">
        /// Die Expression, die für die WHERE-Klausel verwendet werden soll
        /// </param>
        /// <returns>
        /// Gibt an, ob der Vorgang erfolgreich war
        /// </returns>
        public bool DeleteEntities(Expression<Func<T, bool>> expression)
        {
            _logger?.Debug(nameof(DeleteEntities));

            return DeleteEntities(ExpressionConverter.ToQueryCondition(expression, typeof(T)));
        }

        private string GenerateLoggingMessage(SqlCommand command)
        {
            var message = "Execute statement: {";

            message += command.CommandText;
            message += "}";

            if (command.Parameters.Count > 0)
            {
                message += " - {@";
                message += string.Join(", @", command.Parameters.Cast<SqlParameter>().Select(par => par.ParameterName + " = '" + par.Value + "'"));
                message += "}";
            }

            return message;
        }

    }

}
