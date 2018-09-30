﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using RobinManzl.DataAccessLayer.Attributes;
using RobinManzl.DataAccessLayer.Query;

namespace RobinManzl.DataAccessLayer.Internal
{
    
    internal static class ExpressionConverter
    {
        
        public static QueryCondition ToQueryCondition<T>(Expression<Func<T, bool>> expression)
            where T : IEntity, new()
        {
            var root = (BinaryExpression)expression.Body;
            return ParseBinaryExpression(root);
        }

        private static QueryCondition ParseBinaryExpression(BinaryExpression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return new LogicalCondition()
                    {
                        LeftCondition = ParseBinaryExpression((BinaryExpression)exp.Left),
                        RightCondition = ParseBinaryExpression((BinaryExpression)exp.Right),
                        LogicalOperator = exp.NodeType == ExpressionType.And || exp.NodeType == ExpressionType.AndAlso ? LogicalOperator.And : LogicalOperator.Or
                    };

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:

                    var op = Operator.Equals;
                    switch (exp.NodeType)
                    {
                        case ExpressionType.Equal:
                            op = Operator.Equals;
                            break;

                        case ExpressionType.NotEqual:
                            op = Operator.NotEquals;
                            break;

                        case ExpressionType.GreaterThan:
                            op = Operator.GreaterThan;
                            break;

                        case ExpressionType.GreaterThanOrEqual:
                            op = Operator.GreaterThanOrEquals;
                            break;

                        case ExpressionType.LessThan:
                            op = Operator.SmallerThan;
                            break;

                        case ExpressionType.LessThanOrEqual:
                            op = Operator.SmallerThanOrEquals;
                            break;
                    }

                    var property = (MemberExpression)exp.Left;
                    var name = property.Member.Name;

                    var attribute = property.Member.GetCustomAttribute<ColumnAttribute>();
                    if (attribute?.Name != null)
                    {
                        name = attribute.Name;
                    }

                    if (exp.Right is ConstantExpression value)
                    {
                        return new ValueCompareCondition()
                        {
                            AttributeName = name,
                            Operator = op,
                            Value = value
                        };
                    }
                    else
                    {
                        var secondProperty = (MemberExpression)exp.Right;
                        var secondName = secondProperty.Member.Name;

                        var secondAttribute = secondProperty.Member.GetCustomAttribute<ColumnAttribute>();
                        if (secondAttribute?.Name != null)
                        {
                            name = secondAttribute.Name;
                        }

                        return new ColumnCompareCondition()
                        {
                            FirstAttributeName = name,
                            Operator = op,
                            SecondAttributeName = secondName
                        };
                    }
                    
                default:
                    throw new ArgumentOutOfRangeException($"This type of expression is currently not supported: {exp.NodeType}");
            }
        }

    }

}
