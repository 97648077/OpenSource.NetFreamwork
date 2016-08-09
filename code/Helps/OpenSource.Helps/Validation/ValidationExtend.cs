using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OpenSource.Helps
{
    /// <summary>
    /// 实体类验证
    /// </summary>
    static public class ValidationExtend
    {
        /// <summary>
        /// 扩展验证
        /// </summary>
        /// <param name="obj">实体类</param>
        /// <param name="field">验证字段</param>
        /// <returns></returns>
        static public ValidtationResult Validation<T>(this object obj, Expression<Func<T, object>> field = null)
        {
            var fieldary = field == null ? new List<string>() : ExpressionHelper.ExpressionRouter(field.Body).Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var allPropertie = obj.GetType().GetProperties();

            #region 验证是否为空  RequiredAttribute
            var requiredProperties = allPropertie.Where(p => InitBuilderValidationWhere<RequiredAttribute>(p, fieldary)).Where(c => c.GetValue(obj) == null).ToList();

            if (requiredProperties.Count > 0)
                return new ValidtationResult
                {
                    Error = GetCustomAttributesFirst<RequiredAttribute>(requiredProperties[0]).ErrorMessage
                };

            #endregion

            #region 验证字符串长度 StringLengthAttribute
            var stringLengthProperties = allPropertie.Where(p => InitBuilderValidationWhere<StringLengthAttribute>(p, fieldary));

            foreach (PropertyInfo stringLengthProperty in stringLengthProperties)
            {
                StringLengthAttribute stringAttri = GetCustomAttributesFirst<StringLengthAttribute>(stringLengthProperty);
                var value = stringLengthProperty.GetValue(obj).ToString();
                if (string.IsNullOrEmpty(value) || value.Length > stringAttri.MaximumLength)
                    return new ValidtationResult { Error = stringAttri.ErrorMessage };
            }
            #endregion

            #region 验证范围 RangeAttribute
            var rangeProperties = allPropertie.Where(p => InitBuilderValidationWhere<RangeAttribute>(p, fieldary));
            foreach (var rangeProperty in rangeProperties)
            {
                RangeAttribute rangeAttri = GetCustomAttributesFirst<RangeAttribute>(rangeProperty);
                var value = rangeProperty.GetValue(obj);
                if (rangeProperty.PropertyType == typeof(string))
                {
                    string s = value.ToString();
                    if (string.IsNullOrEmpty(s) || s.Length > rangeAttri.Maximum.ConvertToIntSafe() || s.Length < rangeAttri.Minimum.ConvertToIntSafe())
                        return new ValidtationResult { Error = rangeAttri.ErrorMessage };
                }
                else
                {
                    decimal d = value.ToDecimalSafe();
                    if (d < Convert.ToDecimal(rangeAttri.Minimum) || d > Convert.ToDecimal(rangeAttri.Minimum))
                        return new ValidtationResult { Error = rangeAttri.ErrorMessage };
                }
            }
            #endregion

            return new ValidtationResult { Succeed = true };
        }

        static private T GetCustomAttributesFirst<T>(PropertyInfo rangeProperty) where T : Attribute
        {
            return rangeProperty.GetCustomAttributes<T>().First();
        }

        static private bool InitBuilderValidationWhere<T>(PropertyInfo p, List<string> fieldary) where T : Attribute
        {
            if (null == fieldary || fieldary.Count == 0) return p.GetCustomAttributes<T>().Any();
            return p.GetCustomAttributes<T>().Any() && fieldary.Contains(p.Name);
        }
    }
}
