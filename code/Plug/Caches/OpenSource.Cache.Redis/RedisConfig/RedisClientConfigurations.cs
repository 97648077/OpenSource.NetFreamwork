
namespace OpenSource.Cache.Redis
{
    public static class RedisClientConfigurations
    {
        private static string _url = "127.0.0.1";

        /// <summary>
        /// Redis连接地址
        /// </summary>
        public static string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        private static int _port = 6379;

        /// <summary>
        /// Redis连接端口
        /// </summary>
        public static int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        private static int _connectTimeout = 10000;

        /// <summary>
        /// Redis连接超时
        /// </summary>
        public static int ConnectTimeout
        {
            get { return _connectTimeout; }
            set { _connectTimeout = value; }
        }

        private static int _connectRetry = 3;
        /// <summary>
        /// Redis连接失败时重复连接次数
        /// </summary>
        public static int ConnectRetry
        {
            get { return _connectRetry; }
            set { _connectRetry = value; }
        }

       
        private static int _defaultDatabase = 0;
        /// <summary>
        /// Redis默认连接库 db0/db1/...
        /// </summary>
        public static int DefaultDatabase
        {
            get { return _defaultDatabase; }
            set { _defaultDatabase = value; }
        }

        private static bool _preserveAsyncOrder = false;
        /// <summary>
        /// 指定消息是并行还是有序的
        /// </summary>
        public static bool PreserveAsyncOrder
        {
            get { return _preserveAsyncOrder; }
            set { _preserveAsyncOrder = value; }
        }
    }
}
