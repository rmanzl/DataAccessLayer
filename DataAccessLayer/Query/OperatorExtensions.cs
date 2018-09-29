
namespace EntityDb.DataAccessLayer.Query
{

    internal static class OperatorExtensions
    {

        internal static string ConvertToSql(this Operator op)
        {
            switch (op)
            {
                case Operator.Equals:
                    return "=";

                case Operator.GreaterThan:
                    return ">";

                case Operator.GreaterThanOrEquals:
                    return ">=";

                case Operator.SmallerThan:
                    return "<";

                case Operator.SmallerThanOrEquals:
                    return "<=";

                case Operator.NotEquals:
                    return "<>";

                case Operator.Like:
                    return "LIKE";

                case Operator.NotLike:
                    return "NOT LIKE";

                default:
                    return null;
            }
        }

    }

}
