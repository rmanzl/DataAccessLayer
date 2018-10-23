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

        internal readonly object Lock;

        internal readonly SqlConnection Connection;

        internal readonly ILogger Logger;

        internal readonly bool IsView;

        internal readonly bool HasIdentityColumn;

        internal readonly string ProcedureSchema;

        internal readonly string InsertProcedure;

        internal readonly string UpdateProcedure;

        internal readonly string DeleteProcedure;

        internal SqlTransaction CurrentTransaction;

        internal readonly PropertyInfo PrimaryKeyProperty;

        internal readonly QueryComponent<T> QueryComponent;

        internal readonly DataManipulationComponent<T> DataManipulationComponent;

        internal readonly ScriptGenerator<T> ScriptGenerator;

        internal readonly EntityParser<T> EntityParser;

        /// <summary>
        /// Beinhaltet die zuletzt generierte Fehlermeldung, sofern eine existiert
        /// </summary>
        public string LastErrorMessage { get; internal set; }

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
            Lock = new object();

            Connection = connection;

            Logger = logger;
            Logger?.Info($"Creating DbService for entity {typeof(T).FullName}");

            TableBaseAttribute attribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (attribute == null)
            {
                attribute = typeof(T).GetCustomAttribute<ViewAttribute>();
            }

            HasIdentityColumn = attribute?.HasIdentityColumn ?? true;

            if (attribute is ViewAttribute viewAttribute)
            {
                IsView = true;
                ProcedureSchema = viewAttribute.ProcedureSchema;
                InsertProcedure = viewAttribute.InsertProcedure;
                UpdateProcedure = viewAttribute.UpdateProcedure;
                DeleteProcedure = viewAttribute.DeleteProcedure;
            }

            var properties = GetProperties();

            var primaryKeyName = "Id";
            foreach (var property in properties)
            {
                Logger?.Debug($"Entity {typeof(T).Name} contains property '{property.Name}' mapping db field [{property.GetCustomAttribute<ColumnAttribute>().Name ?? property.Name}]");

                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    PrimaryKeyProperty = property;
                    primaryKeyName = property.Name;
                }
            }

            if (PrimaryKeyProperty == null)
            {
                throw new InvalidEntityClassException();
            }

            Logger?.Debug($"Primary key property of entity {typeof(T).Name}: {PrimaryKeyProperty.Name}");

            QueryComponent = new QueryComponent<T>(this);
            DataManipulationComponent = new DataManipulationComponent<T>(this);
            ScriptGenerator = new ScriptGenerator<T>(properties, primaryKeyName, attribute);
            EntityParser = new EntityParser<T>(properties);
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

        private List<PropertyInfo> GetProperties()
        {
            var properties = typeof(T).GetRuntimeProperties();

            return properties.Where(prop => prop.GetCustomAttribute<ColumnAttribute>() != null)
                             .ToList();
        }

        internal string GenerateLoggingMessage(SqlCommand command)
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
            Logger?.Debug(nameof(BeginTransaction));

            lock (Lock)
            {
                Logger?.Info("Beginning transaction");

                if (Connection.State != ConnectionState.Open)
                {
                    Logger?.Info("Opening connection");
                    Connection.Open();
                }

                CurrentTransaction = transaction ?? Connection.BeginTransaction();

                return CurrentTransaction;
            }
        }

        /// <summary>
        /// Entfernt die aktuelle Transaktion aus diesem DbService
        /// </summary>
        public void RemoveTransaction()
        {
            Logger?.Debug(nameof(RemoveTransaction));

            lock (Lock)
            {
                Logger?.Info("Removing transaction");

                CurrentTransaction = null;

                if (Connection.State == ConnectionState.Open)
                {
                    Logger?.Info("Closing connection");
                    Connection.Close();
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
            return QueryComponent.GetEntities(queryCondition, options);
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
            return QueryComponent.GetEntities(expression, options);
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
            return QueryComponent.GetEntities(options);
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
            return QueryComponent.GetEntityById(id);
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
            return QueryComponent.TryGetEntityById(id);
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
            return QueryComponent.GetTopNEntities(count, queryCondition);
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
            return QueryComponent.GetTopNEntities(count, expression);
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
            return QueryComponent.GetFirstEntity(queryCondition);
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
            return QueryComponent.GetFirstEntity(expression);
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
            return QueryComponent.GetFirstOrDefaultEntity(queryCondition);
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
            return QueryComponent.GetFirstOrDefaultEntity(expression);
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
            return DataManipulationComponent.InsertEntity(entity);
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
            return DataManipulationComponent.UpdateEntity(entity);
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
            return DataManipulationComponent.DeleteEntity(entityId);
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
            return DataManipulationComponent.DeleteEntities(queryCondition);
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
            return DataManipulationComponent.DeleteEntities(expression);
        }

    }

}
