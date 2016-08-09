using System;

namespace OpenSource.Helps
{
    public class Resulting<T>
    {
        /// <summary>
        /// 此次执行是否成功
        /// </summary>
        public Boolean succeed { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error { get; set; }
    }
}
