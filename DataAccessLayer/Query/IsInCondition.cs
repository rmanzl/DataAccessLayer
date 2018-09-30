using System;
using System.Collections.Generic;
using System.Text;

namespace RobinManzl.DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit angegebenen Werten dar
    /// </summary>
    public sealed class IsInCondition : QueryCondition
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
        /// Die Angabe der Werte, auf welche geprüft werden soll
        /// </summary>
        public List<object> Values { get; set; }

        /// <summary>
        /// Gibt an, ob der Sapltenwert in den angebenen Werten enthalten sein muss, oder ob er nicht enthalten sein darf
        /// </summary>
        public bool ShouldBeIn { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameters)
        {
            if (Values.Count == 0)
            {
                stringBuilder.Append("1 = 0");
                return;
            }

            stringBuilder.Append("[");
            stringBuilder.Append(TableName);
            stringBuilder.Append("].");

            stringBuilder.Append("[");
            stringBuilder.Append(AttributeName);
            stringBuilder.Append("] ");

            if (!ShouldBeIn)
            {
                stringBuilder.Append("NOT ");
            }

            stringBuilder.Append("IN (@");

            for(int i = 0; i < Values.Count; i++)
            {
                var attributeName = AttributeName + Guid.NewGuid()
                                                    .ToString()
                                                    .Replace("-", string.Empty);

                stringBuilder.Append(attributeName);
                parameters.Add(attributeName, Values[i]);

                if(i != Values.Count - 1)
                {
                    stringBuilder.Append(", @");
                }
            }

            stringBuilder.Append(")");
        }

    }

}
