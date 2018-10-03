using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Query;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class ScriptGenerator<T>
        where T : IEntity, new()
    {

        private readonly List<PropertyInfo> _properties;

        private readonly List<PropertyInfo> _tableSpecificProperties;

        private readonly string _tableName;

        private readonly string _primaryKeyName;

        private string _selectQuery;

        private string _insertQuery;

        private string _updateQuery;

        private string _deleteQuery;

        public ScriptGenerator(List<PropertyInfo> properties)
        {
            _properties = properties;

            foreach (var property in properties)
            {
                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    _primaryKeyName = property.Name;
                    break;
                }
            }

            _tableSpecificProperties = _properties.Where(prop => !prop.Name.Equals(_primaryKeyName)).ToList();

            TableBaseAttribute attribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (attribute == null)
            {
                attribute = typeof(T).GetCustomAttribute<ViewAttribute>();
            }

            if (attribute != null)
            {
                _tableName = "";
                if (attribute.Schema != null)
                {
                    _tableName += attribute.Schema + "].[";
                }

                if (attribute.Name != null)
                {
                    _tableName += attribute.Name;
                }
                else
                {
                    _tableName += typeof(T).Name;
                }
            }
            else
            {
                _tableName = typeof(T).Name;
            }
        }

        public string GetSelectQuery()
        {
            if (_selectQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append("SELECT{0} [");
                stringBuilder.Append(_tableName);
                stringBuilder.Append("].[");

                stringBuilder.Append(string.Join($"], [{_tableName}].[", _properties.Select(prop =>
                {
                    var attribute = prop.GetCustomAttribute<ColumnAttribute>();
                    return attribute.Name ?? prop.Name;
                })));
                stringBuilder.AppendLine("]");

                stringBuilder.Append("FROM [");
                stringBuilder.Append(_tableName);
                stringBuilder.AppendLine("]");

                _selectQuery = stringBuilder.ToString();
            }

            return _selectQuery;
        }

        public string GetSelectQuery(Dictionary<string, object> parameters, QueryCondition queryCondition = null, QueryOptions queryOptions = null)
        {
            var selectQuery = GetSelectQuery();
            if (queryOptions != null &&
                queryOptions.MaxRowCount != null)
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

            if (queryOptions?.OrderByColumn1 != null)
            {
                stringBuilder.Append($"ORDER BY {queryOptions.OrderByColumn1}");

                if (queryOptions.OrderByColumn2 != null)
                {
                    stringBuilder.Append($", {queryOptions.OrderByColumn2}");

                    if (queryOptions.OrderByColumn3 != null)
                    {
                        stringBuilder.Append($", {queryOptions.OrderByColumn3}");
                    }
                }

                stringBuilder.AppendLine($" {(queryOptions.OrderDescending ? "DESC" : "ASC")}");
            }

            return stringBuilder.ToString();
        }

        public string GetInsertQuery()
        {
            if (_insertQuery == null)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append("INSERT INTO [");
                stringBuilder.Append(_tableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append("([");
                stringBuilder.Append(string.Join("], [", _tableSpecificProperties.Select(prop =>
                {
                    var attribute = prop.GetCustomAttribute<ColumnAttribute>();
                    return attribute.Name ?? prop.Name;
                })));
                stringBuilder.AppendLine("])");

                stringBuilder.AppendLine($"OUTPUT INSERTED.[{_primaryKeyName}]");
                stringBuilder.AppendLine("VALUES");

                stringBuilder.Append("(@");
                stringBuilder.Append(string.Join(", @", _tableSpecificProperties.Select(prop => prop.Name)));
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
                stringBuilder.Append(_tableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append("SET [");
                stringBuilder.AppendLine(string.Join(", [", _tableSpecificProperties.Select(prop =>
                {
                    var attribute = prop.GetCustomAttribute<ColumnAttribute>();
                    var name = attribute.Name ?? prop.Name;
                    return name + "] = @" + name;
                })));

                stringBuilder.Append($"WHERE [{_primaryKeyName}] = @{_primaryKeyName}");

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
                stringBuilder.Append(_tableName);
                stringBuilder.AppendLine("]");

                stringBuilder.Append($"WHERE[{ _primaryKeyName}] = @{ _primaryKeyName}");

                _deleteQuery = stringBuilder.ToString();
            }

            return _deleteQuery;
        }

        public string GetDeleteQuery(Dictionary<string, object> parameters, QueryCondition queryCondition)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("DELETE FROM [");
            stringBuilder.Append(_tableName);
            stringBuilder.AppendLine("]");

            stringBuilder.Append("WHERE ");
            queryCondition.GenerateConditionString(stringBuilder, parameters);
            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

    }

}
