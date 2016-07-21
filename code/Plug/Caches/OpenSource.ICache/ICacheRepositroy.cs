using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSource.ICache
{
    /// <summary>
    /// 缓存插件
    /// </summary>
    public interface ICacheRepositroy
    {
        bool Exists(string key);
        bool Add<T>(string key, T value, TimeSpan expiresAt) where T : class;
        bool Update<T>(string key, T value) where T : class;
        T Get<T>(string key) where T : class;

        T GetExpire<T>(string key,TimeSpan expiresAt) where T : class;
        bool Remove(string key);
    }
}
