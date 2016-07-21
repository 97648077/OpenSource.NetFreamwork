using System;
using OpenSource.Helps;
using OpenSource.ICache;
using StackExchange.Redis;

namespace OpenSource.Cache.Redis
{
    public class CacheRepositroy : ICacheRepositroy
    {
        private readonly IDatabase _db;
        private readonly ConnectionMultiplexer _redis;

        public CacheRepositroy()
        {
            const string configuration = "{0},abortConnect=false,defaultDatabase={1},ssl=false,ConnectTimeout={2},allowAdmin=true,connectRetry={3}";
            _redis = ConnectionMultiplexer
                .Connect(string.Format(configuration, RedisClientConfigurations.Url,
                    RedisClientConfigurations.DefaultDatabase, RedisClientConfigurations.ConnectTimeout,
                    RedisClientConfigurations.ConnectRetry));
            _redis.PreserveAsyncOrder = RedisClientConfigurations.PreserveAsyncOrder;
            _db = _redis.GetDatabase();
        }

        public bool Exists(string key)
        {
            return _db.KeyDelete(key);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresAt) where T : class
        {
            return _db.StringSet(key, Helps.Json.ToJson(value), expiresAt);
        }

        public bool Update<T>(string key, T value) where T : class
        {
            return _db.StringSet(key, Helps.Json.ToJson(value));
        }

        public T Get<T>(string key) where T : class
        {
            try
            {
                RedisValue myString = _db.StringGet(key);
                if (myString.HasValue && !myString.IsNullOrEmpty)
                {
                    return Helps.Json.ToObject<T>(myString);
                }
                return null;
            }
            catch (Exception exception)
            {
                // Log Exception
                return null;
            }
        }

        public T GetExpire<T>(string key, TimeSpan expiresAt) where T : class
        {
            var result = Get<T>(key);
            _db.KeyExpire(key, expiresAt);
            return result;
        }

        public bool Remove(string key)
        {
            return _db.KeyExists(key);
        }
    }
}
