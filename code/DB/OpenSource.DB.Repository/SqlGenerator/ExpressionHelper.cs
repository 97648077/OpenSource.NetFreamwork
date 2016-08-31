﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using OpenSource.DB.Repository.Extensions;

namespace OpenSource.DB.Repository.SqlGenerator
{
    internal static class ExpressionHelper
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>The property name for the property expression.</returns>
        public static string GetPropertyName(BinaryExpression body)
        {
            string propertyName = body.Left.ToString().Split('.')[1];

            if (body.Left.NodeType == ExpressionType.Convert)
            {
                // remove the trailing ) when convering.
                propertyName = propertyName.Replace(")", string.Empty);
            }

            return propertyName;
        }

        public static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (Equals(field, null))
            {
                throw new NullReferenceException("Field is required");
            }

            MemberExpression expr = null;

            var body = field.Body as MemberExpression;
            if (body != null)
            {
                expr = body;
            }
            else
            {
                var expression = field.Body as UnaryExpression;
                if (expression != null)
                {
                    expr = (MemberExpression)expression.Operand;
                }
                else
                {
                    string message = $"Expression '{field}' not supported.";

                    throw new ArgumentException(message, nameof(field));
                }
            }

            return expr.Member.Name;
        }

        public static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        public static string GetSqlOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return " AND ";
                case ExpressionType.Equal:
                    return " =";
                case ExpressionType.GreaterThan:
                    return " >";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return " OR ";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";

                case ExpressionType.Default:
                    return string.Empty;

                default:
                    throw new NotImplementedException();
            }
        }

        public static Expression GetExpression(Expression expression)
        {
            return expression;
        }

        public static Func<PropertyInfo, bool> GetPrimitivePropertiesPredicate()
        {
            return p => p.PropertyType.IsValueType() || p.PropertyType.Name.Equals("String", StringComparison.OrdinalIgnoreCase);
        }

        //表达式路由计算 
        public static string ExpressionRouter(Expression exp)
        {
            if (exp is MemberExpression)
            {
                MemberExpression me = ((MemberExpression)exp);
                if (!exp.ToString().StartsWith("value"))
                    return me.Member.Name;
                else
                    return ExpressionRouter(me.Expression);
            }
            else if (exp is NewArrayExpression)
            {
                NewArrayExpression ae = ((NewArrayExpression)exp);
                StringBuilder tmpstr = new StringBuilder();
                foreach (Expression ex in ae.Expressions)
                {
                    tmpstr.Append(ExpressionRouter(ex));
                    tmpstr.Append(",");
                }
                return tmpstr.ToString(0, tmpstr.Length - 1);
            }
            else if (exp is MethodCallExpression)
            {
                MethodCallExpression mce = (MethodCallExpression)exp;
                //return ExpressionRouter(mce);
            }
            else if (exp is ConstantExpression)
            {
                ConstantExpression ce = ((ConstantExpression)exp);
                if (ce.Value == null)
                    return "null";
                else if (ce.Value is ValueType)
                    return ce.Value.ToString();
                else if (ce.Value is string || ce.Value is DateTime || ce.Value is char)
                    return string.Format("'{0}'", ce.Value);

                var type = ce.Value.GetType();
                var propertie = type.GetFields();
                if (propertie.Length > 0)
                {
                    var value = propertie[0].GetValue(ce.Value);
                    if (value is ValueType)
                        return value.ToString();
                    else if (value is string || value is DateTime || value is char)
                        return string.Format("'{0}'", value);
                    else if (value is Array)
                    {
                        var valueAry = value as object[];
                        if (valueAry[0] is ValueType)
                            return string.Join(",", valueAry);
                        else
                            return $"'{string.Join("','", valueAry)}'";
                    }
                }
            }
            else if (exp is UnaryExpression)
            {
                UnaryExpression ue = ((UnaryExpression)exp);
                return ExpressionRouter(ue.Operand);
            }
            return null;
        }
    }
}