using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NLog;
using RobinManzl.DataAccessLayer.Internal;
using RobinManzl.DataAccessLayer.Internal.Model;
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

        private StringBuilder _stringBuilder;

        internal readonly object Lock;

        internal readonly SqlConnection Connection;

        internal readonly ILogger Logger;

        internal SqlTransaction CurrentTransaction;

        internal EntityModel EntityModel;

        internal readonly ModelBuilder ModelBuilder;

        internal readonly QueryComponent<T> QueryComponent;

        internal readonly DataManipulationComponent<T> DataManipulationComponent;

        internal readonly ScriptGenerator ScriptGenerator;

        internal readonly EntityParser<T> EntityParser;

        /// <summary>
        /// Beinhaltet die zuletzt generierte Fehlermeldung, sofern eine existiert
        /// </summary>
        public string LastErrorMessage { get; internal set; }

        private DbService(SqlConnection connection, ILogger logger = null, bool useNLog = false)
        {
            Lock = new object();

            Connection = connection;

            Logger = logger ?? (useNLog ? new NLogWrapper(LogManager.GetCurrentClassLogger()) : null);
            // TODO: modifiy CurrentClass Logger -> append Type-Parameter

            Logger?.Info($"Creating DbService for entity {typeof(T).FullName}");

            ModelBuilder = new ModelBuilder(typeof(T), logger, useNLog);
            EntityModel = ModelBuilder.EntityModel;

            QueryComponent = new QueryComponent<T>(this, EntityModel);
            DataManipulationComponent = new DataManipulationComponent<T>(this, EntityModel);
            ScriptGenerator = ModelBuilder.ScriptGenerator;
            EntityParser = new EntityParser<T>(EntityModel);
        }

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
            : this(connection, logger, false)
        {
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
            : this(connection, new NLogWrapper(logger), false)
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
            : this(connection, useNLog ? new NLogWrapper(LogManager.GetCurrentClassLogger()) : null, true)
        {
        }

        internal string GenerateLoggingMessage(SqlCommand command)
        {
            if (_stringBuilder == null)
            {
                _stringBuilder = new StringBuilder();
            }
            else
            {
                _stringBuilder.Clear();
            }

            _stringBuilder.Append("Execute statement: {");
            _stringBuilder.Append(command.CommandText);
            _stringBuilder.Append("}");

            if (command.Parameters.Count > 0)
            {
                _stringBuilder.Append(" - {@");
                _stringBuilder.Append(string.Join(", @", command.Parameters.Cast<SqlParameter>().Select(par => par.ParameterName + " = '" + par.Value + "'")));
                _stringBuilder.Append("}");
            }

            return _stringBuilder.ToString();
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

        /// <summary>
        /// Diese Methode kann verwendet werden, um den maximalen Wert der angegebenen Spalte zu ermitteln
        /// </summary>
        /// <param name="attributeName">
        /// Das Attribut, in welchem gesucht werden soll
        /// </param>
        /// <param name="queryCondition">
        /// Die QueryConition, die für die WHERE-Klausel verwendet werden soll
        /// </param>
        /// <returns>
        /// Gibt den maximalen Wert zurück
        /// </returns>
        public TValue GetMax<TValue>(string attributeName, QueryCondition queryCondition = null)
            where TValue : struct
        {
            return QueryComponent.GetMax<TValue>(attributeName, queryCondition);
        }

        /// <summary>
        /// Diese Methode kann verwendet werden, um den maximalen Wert der angegebenen Spalte zu ermitteln
        /// </summary>
        /// <param name="attributeName">
        /// Das Attribut, in welchem gesucht werden soll
        /// </param>
        /// <param name="expression">
        /// Die Expression, die für die WHERE-Klausel verwendet werden soll
        /// </param>
        /// <returns>
        /// Gibt den maximalen Wert zurück
        /// </returns>
        public TValue GetMax<TValue>(string attributeName, Expression<Func<T, bool>> expression)
        {
            return QueryComponent.GetMax<TValue>(attributeName, expression);
        }

    }

}
