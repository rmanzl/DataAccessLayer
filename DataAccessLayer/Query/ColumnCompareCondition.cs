using System.Collections.Generic;
using System.Text;

namespace RobinManzl.DataAccessLayer.Query
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt eine Bedingung mit einem Wertvergleich einer Spalte mit einer anderen Spalte dar
    /// </summary>
    public sealed class ColumnCompareCondition : QueryCondition
    {

        /// <inheritdoc />
        public override ConditionType ConditionType => ConditionType.ValueCompare;

        /// <summary>
        /// Die Angabe des ersten Spaltennamens
        /// </summary>
        public string FirstAttributeName { get; set; }

        /// <summary>
        /// Die Angabe des zweiten Spaltennamens
        /// </summary>
        public string SecondAttributeName { get; set; }

        /// <summary>
        /// Der Vergleichsoperator, der für einen Wert- oder Spaltenvergleich verwendet werden soll
        /// </summary>
        public Operator Operator { get; set; }

        internal override void GenerateConditionString(StringBuilder stringBuilder, Dictionary<string, object> parameterse)
        {
            stringBuilder.Append("[");
            stringBuilder.Append(FirstAttributeName);
            stringBuilder.Append("] ");

            stringBuilder.Append(Operator.ConvertToSql());

            stringBuilder.Append("[");
            stringBuilder.Append(SecondAttributeName);
            stringBuilder.Append("]");
        }

    }

}
