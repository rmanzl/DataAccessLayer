using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Internal;
using RobinManzl.DataAccessLayer.Query;

namespace RobinManzl.DataAccessLayer
{

    /// <summary>
    /// Diese Klasse kann verwendet werden, um Daten aus einer Datenbank auszulesen und zu ändern
    /// </summary>
    /// <typeparam name="T">
    /// Die Klasse gibt an, für welche Tabelle Daten ausgelesen werden sollen
    /// </typeparam>
    public class DbService<T>
        where T : IEntity, new()
    {

        private readonly object _lock;

        private readonly SqlConnection _connection;

        private readonly ILogger _logger;

        private readonly bool _isView;

        private readonly string _procedureSchema;

        private readonly string _insertProcedure;

        private readonly string _updateProcedure;

        private readonly string _deleteProcedure;

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
            _scriptGenerator = new ScriptGenerator<T>(properties);
            _entityParser = new EntityParser<T>(properties);
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
            lock (_lock)
            {
                _connection.Open();
                _currentTransaction = transaction ?? _connection.BeginTransaction();

                return _currentTransaction;
            }
        }

        /// <summary>
        /// Entfernt die aktuelle Transaktion aus diesem DbService
        /// </summary>
        public void RemoveTransaction()
        {
            lock (_lock)
            {
                _currentTransaction = null;

                if (_connection.State == ConnectionState.Open)
                {
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
            lock (_lock)
            {
                var opened = _connection.State != ConnectionState.Open;

                if (opened)
                {
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

                    _logger?.Debug(GenerateLoggingMessage(command));

                    var reader = command.ExecuteReader();

                    var entities = new List<T>();
                    while (reader.Read())
                    {
                        entities.Add(_entityParser.ParseEntity(reader));
                    }

                    return entities;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
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
            return GetEntities(ExpressionConverter.ToQueryCondition(expression, typeof(T)), options);
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
            var entities = GetEntities(new ValueCompareCondition
            {
                AttributeName = nameof(IEntity.Id),
                Value = id,
                Operator = Operator.Equals
            });

            return entities.Single();
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
            if (_isView &&
                _insertProcedure == null)
            {
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

                    _logger?.Debug(GenerateLoggingMessage(command));

                    var result = (int)command.ExecuteScalar();
                    entity.Id = result;

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
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
            if (_isView &&
                _updateProcedure == null)
            {
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

                    _logger?.Debug(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
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
            if (_isView &&
                _deleteProcedure == null)
            {
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

                    command.Parameters.AddWithValue(nameof(IEntity.Id), entityId);

                    _logger?.Debug(GenerateLoggingMessage(command));

                    command.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exception)
                {
                    _logger?.Error("Error while executing query", exception);
                    LastErrorMessage = exception.Message;

                    if (_currentTransaction != null)
                    {
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
                        _connection.Close();
                    }
                }
            }
        }

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
