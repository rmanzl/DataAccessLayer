using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using RobinManzl.DataAccessLayer.Internal.Model;
using RobinManzl.DataAccessLayer.Query;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class ScriptGenerator
    {

        private readonly ILogger _logger;

        private readonly EntityModel _entityModel;

        private string _selectQuery;

        private string _insertQuery;

        private string _updateQuery;

        private string _deleteQuery;

        public ScriptGenerator(EntityModel entityModel, ILogger logger, bool useNLog)
        {
            _logger = logger ?? (useNLog ? new NLogWrapper(LogManager.GetCurrentClassLogger()) : null);

            _entityModel = entityModel;
        }

        private string GetSelectQuery()
        {
            if (_selectQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append($"SELECT{{0}} [{_entityModel.TableName}].[{_entityModel.PrimaryKeyName}], [");
                stringBuilder.Append(_entityModel.TableName);
                stringBuilder.Append("].[");

                stringBuilder.Append(string.Join($"], [{_entityModel.TableName}].[", _entityModel.Columns.Select(c => c.ColumnName)));
                stringBuilder.AppendLine("]");

                stringBuilder.Append("FROM [");
                stringBuilder.Append(_entityModel.TableName);
                stringBuilder.AppendLine("]");

                _selectQuery = stringBuilder.ToString();
            }

            return _selectQuery;
        }

        public string GetSelectQuery(Dictionary<string, object> parameters, QueryCondition queryCondition = null, QueryOptions queryOptions = null)
        {
            var selectQuery = GetSelectQuery();
            if (queryOptions?.MaxRowCount != null)
            {
                selectQuery = string.Format(selectQuery, " TOP " + queryOptions.MaxRowCount);
            }
            else
            {
                selectQuery = string.Format(selectQuery, string.Empty);
            }

            var stringBuilder = new StringBuilder(selectQuery);

            if (queryCondition != null)
            {
                stringBuilder.Append("WHERE ");
                queryCondition.GenerateConditionString(stringBuilder, parameters);
                stringBuilder.AppendLine();
            }

            if (queryOptions?.OrderByOptions?.Count > 0)
            {
                stringBuilder.Append("ORDER BY ");
                stringBuilder.AppendLine(string.Join(", ", queryOptions.OrderByOptions.Select(o => $"{o.Column} {(o.SortDirection == SortDirection.Descending ? "DESC" : "ASC")}")));
            }

            return stringBuilder.ToString();
        }

        public string GetInsertQuery()
        {
            if (_insertQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append("INSERT INTO [");
                stringBuilder.Append(_entityModel.TableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append("([");
                if (!_entityModel.HasIdentityColumn)
                {
                    stringBuilder.Append($"{_entityModel.PrimaryKeyName}], [");
                }

                stringBuilder.Append(string.Join("], [", _entityModel.Columns.Select(c => c.ColumnName)));
                stringBuilder.AppendLine("])");

                if (_entityModel.HasIdentityColumn)
                {
                    stringBuilder.AppendLine($"OUTPUT INSERTED.[{_entityModel.PrimaryKeyName}]");
                }

                stringBuilder.AppendLine("VALUES");

                stringBuilder.Append("(@");
                if (!_entityModel.HasIdentityColumn)
                {
                    stringBuilder.Append($"{_entityModel.PrimaryKeyProperty.Name}, @");
                }

                stringBuilder.Append(string.Join(", @", _entityModel.Columns.Select(c => c.Property.Name)));
                stringBuilder.Append(")");

                _insertQuery = stringBuilder.ToString();
            }

            return _insertQuery;
        }

        public string GetUpdateQuery()
        {
            if (_updateQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append("UPDATE [");
                stringBuilder.Append(_entityModel.TableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append("SET [");
                stringBuilder.AppendLine(string.Join(", [", _entityModel.Columns.Select(c => $"{c.ColumnName}] = @{c.Property.Name}")));

                stringBuilder.Append($"WHERE [{_entityModel.PrimaryKeyName}] = @{_entityModel.PrimaryKeyProperty.Name}");

                _updateQuery = stringBuilder.ToString();
            }

            return _updateQuery;
        }

        public string GetDeleteQuery()
        {
            if (_deleteQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append("DELETE FROM [");
                stringBuilder.Append(_entityModel.TableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append($"WHERE[{_entityModel.PrimaryKeyName}] = @{_entityModel.PrimaryKeyProperty.Name}");

                _deleteQuery = stringBuilder.ToString();
            }

            return _deleteQuery;
        }

        public string GetDeleteQuery(Dictionary<string, object> parameters, QueryCondition queryCondition)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("DELETE FROM [");
            stringBuilder.Append(_entityModel.TableName);
            stringBuilder.AppendLine("]");

            stringBuilder.Append("WHERE ");
            queryCondition.GenerateConditionString(stringBuilder, parameters);
            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

    }

}
