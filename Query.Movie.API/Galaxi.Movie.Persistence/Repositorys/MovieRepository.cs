using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Galaxi.Query.Movie.Data.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Galaxi.Tickets.Persistence.Repositorys;

namespace Galaxi.Query.Movie.Persistence.Repositorys
{
    public class MovieRepository : IMovieRepository
    {
        private readonly ILogger<MovieRepository> _log;
        private readonly ElasticsearchClient _elasticsearch;
        private readonly ICacheRedis _cache;
        private const string _cacheKeyAllMovies = "movies_all";

        public MovieRepository(ILogger<MovieRepository> log, ElasticsearchClient elasticsearch, ICacheRedis cache)
        {
            _cache = cache;
            _log = log;
            _elasticsearch = elasticsearch;
        }

        public async Task Add(Film movie)
        {
            _log.LogInformation($"Creating new movie with ID (elasticSearch) {movie.FilmId}");
            var addMovie = await _elasticsearch.IndexAsync<Film>(movie, idx => idx.Index("films"));

            await _cache.RemoveCacheAsync(movie.FilmId);
        }

        public async Task Delete(Film movie)
        {
            _log.LogInformation($"Delete movie with movieId (elasticSearch) {movie.FilmId}");

            var searchResponse = await _elasticsearch.SearchAsync<Film>(s => s
               .Index("films")
               .Query(q => q
                   .Term(t => t
                       .Field(f => f.FilmId)
                       .Value(movie.FilmId.ToString())
                   )
               )
           );

            var hit = searchResponse.Hits.FirstOrDefault();

            var deleteResponse = await _elasticsearch.DeleteAsync<Film>(hit.Id, u => u
                .Index("films")
            );

            await _cache.RemoveCacheAsync(movie.FilmId);
        }

        public async Task UpdateMovieAsync(Film movie)
        {
            _log.LogInformation($"Updating movie with movie ID (elasticSearch) {movie.FilmId}");


            var searchResponse = await GetIdDocElasticByMovieId(movie.FilmId);

            var updateResponse = await _elasticsearch.UpdateAsync<ElasticsearchClient, Film>(searchResponse, u => u
                .Index("films")
                .Doc(movie)
            );

            await _cache.RemoveCacheAsync(movie.FilmId);
        }

        protected async Task<string> GetIdDocElasticByMovieId(Guid FilmId)
        {
            var searchResponse = await _elasticsearch.SearchAsync<Film>(s => s
               .Index("films")
               .Query(q => q
                   .Term(t => t
                       .Field(f => f.FilmId)
                       .Value(FilmId.ToString())
                   )
               )
           );

            var hit = searchResponse.Hits.FirstOrDefault();

            return hit.Id;

        }

        public async Task<Film> GetMovieByIdAsync(Guid id)
        {
            var cacheKey = $"movie_{id}";

            var cacheMovie = await _cache.GetCacheAsync<Film>(cacheKey);

            if (cacheMovie != null)
            {
                return cacheMovie;
            }

            var movie = await _elasticsearch.SearchAsync<Film>(s => s
                           .Index("films")
                           .Query(q => q
                               .Term(t => t
                                   .Field(f => f.FilmId)
                                   .Value(id.ToString())
                               )
                           )
                       );

            if (movie.Documents.FirstOrDefault() != null)
            {
                _ = _cache.SetCacheAsync(movie.Documents.FirstOrDefault(), cacheKey);
            }
            return movie.Documents.FirstOrDefault();
        }

        public async Task<IEnumerable<Film>> GetAllMoviesAsync()
        {
            var cacheMovies = await _cache.GetCacheAsync<IEnumerable<Film>>(_cacheKeyAllMovies);

            if (cacheMovies != null)
            {
                return cacheMovies;
            }

            var movies = await _elasticsearch.SearchAsync<Film>(s => s
                                .Index("films")
                                .Query(q => q.MatchAll(Ma => Ma = Ma))
                                .Size(1000)
                            );

            if (movies != null && movies.Documents.Any())
            {
                _ = _cache.SetCacheAsync(movies.Documents, _cacheKeyAllMovies);
            }

            return movies.Documents;
        }

        public async Task<IEnumerable<Film>> GetMovieByQuery(string query)
        {
            string cacheQueryKey = $"Movie_query_{query}";

            var cacheMovies = await _cache.GetCacheAsync<IEnumerable<Film>>(cacheQueryKey);

            if (cacheMovies != null)
            {
                return cacheMovies;
            }

            var searchMovieResponse = await _elasticsearch.SearchAsync<Film>(s => s
                    .Query(q => q
                        .SimpleQueryString(sqs => sqs
                            .Query($"*{query}*")
                            .Fields(new[] { "title^3", "genre^2", "description^1" })
                            .DefaultOperator(Operator.And)
                            .AnalyzeWildcard(true)
                        )
                    )
                );

            if (searchMovieResponse != null && searchMovieResponse.Documents.Any())
            {
                _ = _cache.SetCacheAsync(searchMovieResponse.Documents, cacheQueryKey);
            }

            return searchMovieResponse.Documents;
        }

        public async Task MigrateELKAsync(IEnumerable<Film> films)
        {
            try
            {
                await _elasticsearch.Indices.CreateAsync<Film>("films", c => c
                     .Mappings(m => m
                         .Properties(p => p
                             .Keyword(k => k
                                 .FilmId
                                 ))
              ));
            }
            catch (Exception)
            {
                _log.LogWarning($"Failed to create index to the ElasticSearch");
            }

            _log.LogInformation($"Creating {films.Count()} films in bulk");

            foreach (var film in films)
            {
                if (film.FilmId == Guid.Empty)
                {
                    film.FilmId = Guid.NewGuid();
                }
            }

            try
            {
                var bulkResponse = await _elasticsearch.BulkAsync(b => b
                           .Index("films")
                           .IndexMany(films, (descriptor, film) => descriptor
                               .Index("films")
                               .Id(film.FilmId.ToString())
                           )
                       );
            }
            catch (Exception)
            {
                _log.LogError($"Failed to insert documents into elasticSearch");
            }

            _log.LogInformation($"{films.Count()} movies were created successfully");
        }
    }
}
