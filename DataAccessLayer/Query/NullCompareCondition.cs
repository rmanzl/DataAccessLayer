using System.Collections.Generic;
using System.Text;

namespace RobinManzl.DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte auf NULL dar
    /// </summary>
    public sealed class NullCompareCondition : QueryCondition
    {

        /// <inheritdoc />
        public override ConditionType ConditionType => ConditionType.NullCompare;

        /// <summary>
        /// Die Angabe des Tabellennamens
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Die Angabe des Spaltennamens
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// Gibt an, ob der Wert der Spalte auf NULL oder NOT NULL geprüft werden soll
        /// </summary>
        public bool ShouldBeNull { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameter, string tableName)
        {
            if (TableName == null)
            {
                TableName = tableName;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] IS");

            if (!ShouldBeNull)
            {
                stringBuilder.Append(" NOT");
            }
            stringBuilder.Append(" NULL");
        }

    }

}
