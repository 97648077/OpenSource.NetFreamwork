﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace OpenSource.DB.Repository.SqlGenerator
{
    public class PropertyMetadata
    {
        public PropertyInfo PropertyInfo { get; }

        public string Alias { get; }

        public string ColumnName => string.IsNullOrEmpty(this.Alias) ? this.PropertyInfo.Name : this.Alias;

        public string Name => PropertyInfo.Name;

        public PropertyMetadata(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;
            var alias = this.PropertyInfo.GetCustomAttribute<ColumnAttribute>();
            this.Alias = alias != null ? alias.Name : string.Empty;
        }
    }
}