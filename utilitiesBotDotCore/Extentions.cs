using CacheManager.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace utilitiesBotDotCore
{
    public static class Extentions
    {
        public static void Put<T>(this ICacheManager<T> cache,string key,T value,TimeSpan time)
        {
            cache.Put(new CacheItem<T>(key, value, ExpirationMode.Absolute, time));
        }
    }
}
