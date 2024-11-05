﻿using Galaxi.Query.Movie.Data.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;
using StackExchange.Redis;

namespace Galaxi.Tickets.Persistence.Repositorys
{
    public class CacheRedis : ICacheRedis
    {
        private readonly ILogger<CacheRedis> _log;
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
        private const string _cacheKeyAllMovies = "movies_all";

        public CacheRedis(ILogger<CacheRedis> log, IDistributedCache cache)
        {
            _log = log;
            _cache = cache;
        }

        public async Task SetCacheAsync<T>(T entity, string cacheKey)
        {
            try
            {
                await Policy.TimeoutAsync(TimeSpan.FromMilliseconds(600), TimeoutStrategy.Pessimistic).ExecuteAsync(async () =>

                {
                    await _cache.SetStringAsync
                            (cacheKey, JsonConvert.SerializeObject(entity),
                              new DistributedCacheEntryOptions
                              {
                                  AbsoluteExpirationRelativeToNow = _cacheExpiration
                              }
                            );
                });
            }
            catch (Exception ex)
            {
                _log.LogWarning("Failed to set cache in Redis", ex);
            }

        }
        public async Task<T> GetCacheAsync<T>(string cacheKey) where T : class
        {
            try
            {
                string cachedData = null;

                await Policy.TimeoutAsync(TimeSpan.FromMilliseconds(600), TimeoutStrategy.Pessimistic).ExecuteAsync(async () =>
                {
                    cachedData = await _cache.GetStringAsync(cacheKey);
                });

                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonConvert.DeserializeObject<T>(cachedData);
                }
            }
            catch (RedisException ex)
            {
                _log.LogWarning($"Failed to retrieve cache in Redis for key {cacheKey}", ex);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"An unexpected error occurred when retrieving cache for key {cacheKey}", ex);
            }
            return null;
        }
        public async Task RemoveCacheAsync(Guid? filmId = null)
        {
            try
            {
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic).ExecuteAsync(async () =>
                {
                    await Task.WhenAll(
                       _cache.RemoveAsync($"movie_{filmId}"),
                       _cache.RemoveAsync(_cacheKeyAllMovies)
                   );
                });
            }
            catch (RedisException ex)
            {
                _log.LogWarning($"Failed to remove cache in Redis for movie {filmId}", ex);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"An unexpected error occurred when removing cache for movie {filmId}", ex);
            }
        }

    }
}
