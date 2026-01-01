using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                return cachedValue;
            }
            return default(T);
        }

        public Task SetAsync<T>(string key, T value, System.TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            
            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            // IMemoryCache doesn't have a Clear method
            // For production, consider using IDistributedCache or a custom cache implementation
            // that supports clearing all entries
            return Task.CompletedTask;
        }
    }
}
