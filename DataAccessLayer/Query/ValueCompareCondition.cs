using System;
using System.Collections.Generic;
using System.Text;

namespace RobinManzl.DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit einem angegebenen Wert dar
    /// </summary>
    public sealed class ValueCompareCondition : QueryCondition
    {

        /// <inheritdoc />
        public override ConditionType ConditionType => ConditionType.ValueCompare;

        /// <summary>
        /// Die Angabe des Tabellennamens
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Die Angabe des Spaltennamens
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// Die Angabe des Wertes
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Der Vergleichsoperator, der für einen Wert- oder Spaltenvergleich verwendet werden soll
        /// </summary>
        public Operator Operator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters, string tableName)
        {
            if (TableName == null)
            {
                TableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] ");

            stringBuilder.Append(Operator.ConvertToSql());
            stringBuilder.Append(" @");

            var attributeName = AttributeName + Guid.NewGuid()
                                                    .ToString()
                                                    .Replace("-", string.Empty);
            stringBuilder.Append(attributeName);

            parameters.Add(attributeName, Value);
        }

    }

}
