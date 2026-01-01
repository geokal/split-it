using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, System.TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task ClearAsync();
    }
}
