
namespace Galaxi.Tickets.Persistence.Repositorys
{
    public interface ICacheRedis
    {
        Task<T> GetCacheAsync<T>(string cacheKey) where T : class;
        Task RemoveCacheAsync(Guid? filmId = null);
        Task SetCacheAsync<T>(T entity, string cacheKey);
    }
}