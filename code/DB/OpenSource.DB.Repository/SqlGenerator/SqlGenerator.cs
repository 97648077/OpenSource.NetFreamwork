using OpenSource.DB.Repository.Attributes;
using OpenSource.DB.Repository.Extensions;
using OpenSource.Helps.DB.DbAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSource.DB.Repository.SqlGenerator
{
    public class SqlGenerator<TEntity> : ISqlGenerator<TEntity>
        where TEntity : class
    {
        public SqlGenerator(ESqlConnector sqlConnector)
        {
            SqlConnector = sqlConnector;
            var entityType = typeof(TEntity);
            var entityTypeInfo = entityType.GetTypeInfo();
            var aliasAttribute = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            this.TableName = aliasAttribute != null ? aliasAttribute.Name : entityTypeInfo.Name;
            AllProperties = entityType.GetProperties();
            //Load all the "primitive" entity properties
            var props = AllProperties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

            //Filter the non stored properties
            this.BaseProperties =
                props.Where(p => !p.GetCustomAttributes<NotMappedAttribute>().Any())
                    .Select(p => new PropertyMetadata(p));

            //Filter key properties
            this.KeyProperties =
                props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any()).Select(p => new PropertyMetadata(p));

            //Use identity as key pattern
            var identityProperty = props.FirstOrDefault(p => p.GetCustomAttributes<IdentityAttribute>().Any());
            this.IdentityProperty = identityProperty != null ? new PropertyMetadata(identityProperty) : null;

            //Status property (if exists, and if it does, it must be an enumeration)
            var statusProperty = props.FirstOrDefault(p => p.GetCustomAttributes<StatusAttribute>().Any());

            if (statusProperty == null) return;
            StatusProperty = new PropertyMetadata(statusProperty);

            if (statusProperty.PropertyType.IsBool())
            {
                var deleteProperty = props.FirstOrDefault(p => p.GetCustomAttributes<DeletedAttribute>().Any());
                if (deleteProperty == null) return;

                LogicalDelete = true;
                LogicalDeleteValue = 1; // true

            }
            else if (statusProperty.PropertyType.IsEnum())
            {

                var deleteOption =
                    statusProperty.PropertyType.GetFields()
                        .FirstOrDefault(f => f.GetCustomAttribute<DeletedAttribute>() != null);

                if (deleteOption == null) return;

                var enumValue = Enum.Parse(statusProperty.PropertyType, deleteOption.Name);

                if (enumValue != null)
                    LogicalDeleteValue = Convert.ChangeType(enumValue,
                        Enum.GetUnderlyingType(statusProperty.PropertyType));

                LogicalDelete = true;
            }
        }

        public SqlGenerator()
            : this(ESqlConnector.MSSQL)
        {
        }

        public ESqlConnector SqlConnector { get; set; }

        public bool IsIdentity => this.IdentityProperty != null;

        public bool LogicalDelete { get; }

        public string TableName { get; }

        public PropertyInfo[] AllProperties { get; }

        public PropertyMetadata IdentityProperty { get; }

        public IEnumerable<PropertyMetadata> KeyProperties { get; }

        public IEnumerable<PropertyMetadata> BaseProperties { get; }

        public PropertyMetadata StatusProperty { get; }

        public object LogicalDeleteValue { get; }

        public virtual SqlQuery GetInsert(TEntity entity)
        {
            List<PropertyMetadata> properties = (this.IsIdentity
                ? this.BaseProperties.Where(
                    p => !p.Name.Equals(this.IdentityProperty.Name, StringComparison.OrdinalIgnoreCase))
                : this.BaseProperties).ToList();

            string columNames = string.Join(", ", properties.Select(p => $"{p.ColumnName}"))
            ;
            string values = string.Join(", ", properties.Select(p => $"@{p.Name}"))
            ;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat("INSERT INTO {0} {1} {2} ",
                this.TableName,
                string.IsNullOrEmpty(columNames) ? "" : $"({columNames})", string.IsNullOrEmpty(values) ? "" : $" VALUES ({values})")
            ;

            if (this.IsIdentity)
            {
                switch (SqlConnector)
                {
                    case ESqlConnector.MSSQL:
                        sqlBuilder.Append("SELECT SCOPE_IDENTITY() AS " + this.IdentityProperty.ColumnName);
                        break;

                    case ESqlConnector.MySQL:
                        sqlBuilder.Append("; SELECT CONVERT(LAST_INSERT_ID(), SIGNED INTEGER) AS " +
                                          this.IdentityProperty.ColumnName);
                        break;

                    case ESqlConnector.PostgreSQL:
                        sqlBuilder.Append("RETURNING " + this.IdentityProperty.ColumnName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new SqlQuery(sqlBuilder.ToString(), entity);
        }

        public virtual SqlQuery GetUpdate(TEntity entity, Expression<Func<TEntity, object>> field = null)
        {
            var properties =
               this.BaseProperties.Where(
                   p => !this.KeyProperties.Any(k => k.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)));

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat("UPDATE {0} SET {1} WHERE {2}", this.TableName, BuilderUpdate(properties, field), string.Join(" AND ", this.KeyProperties.Select(p => $"{p.ColumnName} = @{p.Name}")));

            return new SqlQuery(sqlBuilder.ToString().TrimEnd(), entity);
        }

        public virtual SqlQuery GetUpdate(TEntity entity, Expression<Func<TEntity, object>> field, Expression<Func<TEntity, bool>> predicate)
        {
            var properties =
                this.BaseProperties.Where(
                    p => !this.KeyProperties.Any(k => k.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)));

 
            var sqlBuilder = new StringBuilder();

            sqlBuilder.AppendFormat("UPDATE {0} SET {1} ", this.TableName, BuilderUpdate(properties, field,"x1"));

            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var propertyInfo in AllProperties)
                expando.Add(propertyInfo.Name + "x1", propertyInfo.GetValue(entity));

            BuilderPedicate(predicate, sqlBuilder, ref expando);

            return new SqlQuery(sqlBuilder.ToString().TrimEnd(), expando);
        }

        private string BuilderUpdate(IEnumerable<PropertyMetadata> properties, Expression<Func<TEntity, object>> field,string endStr="")
        {
            if (null == field) return string.Join(",", properties.Select(p => $"{p.ColumnName}{endStr} = @{p.Name}{endStr}"));

            var fieldary = ExpressionHelper.ExpressionRouter(field.Body).Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return string.Join(",", fieldary.Select(p=> $"{p}=@{p}{endStr}"));
        }

        public virtual SqlQuery GetDelete(TEntity entity)
        {
            var sqlBuilder = new StringBuilder();
            if (!LogicalDelete)
            {
                sqlBuilder.AppendFormat("DELETE FROM {0} WHERE {1}", this.TableName,
                    string.Join(" AND ", this.KeyProperties.Select(p => $"{p.ColumnName} = @{p.Name}")));
            }
            else
            {
                sqlBuilder.AppendFormat("UPDATE {0} SET {1} WHERE {2}", this.TableName, $"{this.StatusProperty.ColumnName} = {this.LogicalDeleteValue}", string.Join(" AND ", this.KeyProperties.Select(p => $"{p.ColumnName} = @{p.Name}")));
            }
            return new SqlQuery(sqlBuilder.ToString(), entity);
        }
        public virtual SqlQuery GetDelete(Expression<Func<TEntity, bool>> predicate)
        {
            var sqlBuilder = new StringBuilder();
            if (!LogicalDelete)
            {
                sqlBuilder.AppendFormat("DELETE FROM {0}", this.TableName);
            }
            else
            {
                sqlBuilder.AppendFormat("UPDATE {0} SET {1}", this.TableName, $"{this.StatusProperty.ColumnName} = {this.LogicalDeleteValue}");
            }
            IDictionary<string, object> expando = new ExpandoObject();
            BuilderPedicate(predicate, sqlBuilder, ref expando);

            return new SqlQuery(sqlBuilder.ToString(), expando);
        }

        #region Get Select

        public virtual SqlQuery GetSelectFirst(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            return GetSelect(predicate, true, includes);
        }

        public virtual SqlQuery GetSelectAll(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            return GetSelect(predicate, false, includes);
        }


        private StringBuilder InitBuilderSelect(bool firstOnly)
        {
            var builder = new StringBuilder();
            var select = "SELECT ";

            if (firstOnly && SqlConnector == ESqlConnector.MSSQL)
                select += "TOP 1 ";

            // convert the query parms into a SQL string and dynamic property object
            builder.Append($"{select} {GetFieldsSelect(TableName, BaseProperties)}");

            return builder;
        }


        private static string GetFieldsSelect(string tableName, IEnumerable<PropertyMetadata> properties)
        {
            //Projection function
            Func<PropertyMetadata, string> projectionFunction = (p) => !string.IsNullOrEmpty(p.Alias) ? $"{p.ColumnName} AS {p.Name}" : $"{p.ColumnName}";

            return string.Join(", ", properties.Select(projectionFunction));
        }


        private SqlQuery GetSelect(Expression<Func<TEntity, bool>> predicate, bool firstOnly,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var builder = InitBuilderSelect(firstOnly);

            builder.Append($" FROM {TableName} ");

            IDictionary<string, object> expando = new ExpandoObject();

            BuilderPedicate(predicate, builder, ref expando);

            if (firstOnly && (SqlConnector == ESqlConnector.MySQL || SqlConnector == ESqlConnector.PostgreSQL))
                builder.Append("LIMIT 1");


            return new SqlQuery(builder.ToString().TrimEnd(), expando);
        }

        private void BuilderPedicate(Expression<Func<TEntity, bool>> predicate, StringBuilder builder, ref IDictionary<string, object> expando)
        {
            if (predicate != null)
            {
                // WHERE
                var queryProperties = new List<QueryParameter>();

                FillQueryProperties(ExpressionHelper.GetExpression(predicate.Body), ExpressionType.Default,
                    ref queryProperties);
                builder.Append(" WHERE ");

                for (int i = 0; i < queryProperties.Count; i++)
                {
                    var item = queryProperties[i];
                    if (i == 0)
                        item.LinkingOperator = null;
                    GetQueryParameterQueryOperator(item, ref expando, ref builder);
                }
            }
        }

        private void GetQueryParameterQueryOperator(QueryParameter item, ref IDictionary<string, object> obj, ref StringBuilder builder)
        {
            switch (item.QueryOperator)
            {
                case "In":
                case "Not_In":
                    builder.AppendFormat("{0} {1} ({2}) ", item.PropertyName,
                             item.QueryOperator.Replace("_", " "), SqlFilter(item.PropertyValue.ToString()));
                    break;
                case "Like":
                case "Not_Like":
                    builder.AppendFormat("{0} {1} '{2}' ", item.PropertyName,
                         item.QueryOperator.Replace("_", " "), SqlFilter(item.PropertyValue.ToString()));
                    break;
                case "Is_Null":
                case "Is_Not_Null":
                    builder.AppendFormat("{0} {1}", item.PropertyName, item.QueryOperator.Replace("_", " "));
                    break;
                default:
                    builder.AppendFormat("{0} {1} {2} @{1} ", item.LinkingOperator,
                              item.PropertyName, item.QueryOperator);
                    obj[item.PropertyName] = item.PropertyValue;
                    break;
            }
        }

        private string SqlFilter(string sql)
        {
            var sqlLower = sql.ToLower();
            if ("and|exec|insert|select|delete|update|chr|mid|master|or|truncate|char|declare|join|cmd".Split('|').Any(i => (sqlLower.IndexOf(i + " ", StringComparison.Ordinal) > -1) || (sqlLower.IndexOf(" " + i, StringComparison.Ordinal) > -1)))
                throw new Exception("find sql filter..");
            return sql;
        }

        public virtual SqlQuery GetSelectBetween(object from, object to, Expression<Func<TEntity, object>> btwField,
            Expression<Func<TEntity, bool>> expression)
        {
            var filedName = ExpressionHelper.GetPropertyName(btwField);
            var queryResult = GetSelectAll(expression);
            var op = expression == null ? "WHERE" : "AND";

            queryResult.AppendToSql($" {op} {filedName} BETWEEN '{@from}' AND '{to}'");

            return queryResult;
        }



        public virtual SqlQuery GetSelectCount(string sql, object param)
        {
            return new SqlQuery(string.Format("SELECT COUNT(*) FROM ({0}) Repository", sql), param);
        }

        public virtual SqlQuery GetSelectPages(long from, long to, string sql, object param, Expression<Func<TEntity, object>> field, bool isDesc)
        {
            var orderName = ExpressionHelper.GetPropertyName(field);
            var sqlBuilder = new StringBuilder();

            switch (SqlConnector)
            {
                case ESqlConnector.MSSQL:
                    sqlBuilder.AppendFormat(
                        "SELECT  * FROM ( SELECT ROW_NUMBER() OVER (ORDER BY {0} {4}) AS RowNum,{1})Repository WHERE RowNum BETWEEN {2} AND {3}",
                        orderName, sql.Substring(sql.ToUpper().IndexOf("SELECT") + 6),
                        (from - 1) * to + 1, from * to, isDesc ? "DESC" : string.Empty);
                    break;

                case ESqlConnector.MySQL:
                    sqlBuilder.AppendFormat(
                        "; SELECT * FROM ({0} ORDER BY {1} {4})Repository WHERE  {1} LIMIT {2},{3} ", sql,
                        orderName, from - 1, to, isDesc ? "DESC" : string.Empty);
                    break;

                case ESqlConnector.PostgreSQL:
                    sqlBuilder.AppendFormat("SELECT * FROM ({0} ORDER BY {1})Repository LIMIT {1} OFFSET {2}", sql,
                        to, (from - 1) * to);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new SqlQuery(sqlBuilder.ToString(), param);
        }

        #endregion Get Select

        #region Expression

        /// <summary>
        /// Fill query properties
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="linkingType">Type of the linking.</param>
        /// <param name="queryProperties">The query properties.</param>
        private static void FillQueryProperties(Expression body, ExpressionType linkingType,
            ref List<QueryParameter> queryProperties)
        {
            if (body is BinaryExpression)
            {
                BinaryExpression be = (BinaryExpression)body;
                if (body.NodeType != ExpressionType.AndAlso && body.NodeType != ExpressionType.OrElse)
                {
                    string propertyName = ExpressionHelper.GetPropertyName(be);
                    object propertyValue = ExpressionHelper.GetValue(be.Right);
                    string opr = ExpressionHelper.GetSqlOperator(be.NodeType);
                    string link = ExpressionHelper.GetSqlOperator(linkingType);

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));
                }
                else
                {
                    FillQueryProperties(ExpressionHelper.GetExpression(be.Left), body.NodeType,
                        ref queryProperties);
                    FillQueryProperties(ExpressionHelper.GetExpression(be.Right), body.NodeType,
                        ref queryProperties);

                }
            }
            else if (body is MethodCallExpression)
            {
                MethodCallExpression mce = (MethodCallExpression)body;
                string link = ExpressionHelper.GetSqlOperator(linkingType);
                string opr = mce.Method.Name;
                string propertyName = ExpressionHelper.ExpressionRouter(mce.Arguments[0]);
                object propertyValue = ExpressionHelper.ExpressionRouter(mce.Arguments.Count > 1 ? mce.Arguments[1] : null);
                queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));
            }
        }

    }
    #endregion
}
