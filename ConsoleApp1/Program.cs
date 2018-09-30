using System;
using System.Linq.Expressions;
using System.Reflection;
using RobinManzl.DataAccessLayer;
using RobinManzl.DataAccessLayer.Internal;
using RobinManzl.DataAccessLayer.Query;

namespace ConsoleApp1
{

    public static class Program
    {

        public static void Main(string[] args)
        {
            var y = 1;
            Expression<Func<Column, bool>> exp = column => column.Name == "Test" && column.Test == column.Test && column.Test != y;

            var field = ((BinaryExpression)((BinaryExpression)exp.Body).Right).Right;
            var prop = ((BinaryExpression)((BinaryExpression)exp.Body).Right).Left;

            var queryCondition = ExpressionConverter.ToQueryCondition(exp, typeof(Column));

            var x = 0;
        }

    }

}
