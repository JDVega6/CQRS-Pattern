using System.Reflection;
using MediatR;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using MassTransit;
using Galaxi.Query.Movie.Domain.Profiles;
using Galaxi.Query.Movie.Persistence.Repositorys;
using Elastic.Clients.Elasticsearch;
using Galaxi.Query.Movie.Domain.IntegrationEvents.Consumers;
using Galaxi.Tickets.Persistence.Repositorys;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var service = builder.Services.BuildServiceProvider();
var configuration = service.GetService<IConfiguration>();

builder.Services.AddLogging(logginBuilder =>
{
    //1. Create Config
    var loggerConfig = new LoggerConfiguration()
                           .MinimumLevel.Debug()
                           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                           .Enrich.WithProperty("ElasticSearch", "Enabled")
                           .WriteTo.File
                           (
                                path: "/app/samba/logs/logs-movie-Serilog-.json",
                                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                                rollingInterval: RollingInterval.Day
                           )
                          .WriteTo.Http(builder.Configuration.GetConnectionString("LogStash"), null);

    //2. Create Logger
    var logger = loggerConfig.CreateLogger();

    //3. Inject Service
    logginBuilder.Services.AddSingleton<ILoggerFactory>(
        provider => new SerilogLoggerFactory(logger, dispose: false));

});

builder.Services.AddScoped(provider =>
{
    var settings = new ElasticsearchClientSettings(new Uri(builder.Configuration.GetConnectionString("ElasticSearchConnection")))
        .DefaultIndex("products");

    return new ElasticsearchClient(settings);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CheckAvailableMovieConsumer>();
    x.AddConsumer<MigrationMovieConsumer>();
    x.AddConsumer<UpdateMovieConsumer>();
    x.AddConsumer<AddMovieConsumer>();
    x.AddConsumer<DeleteMovieConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration.GetConnectionString("rabbitMqSettingsHost"), h =>
        {
            h.Username(configuration.GetConnectionString("rabbitMqSettingsUsername"));
            h.Password(configuration.GetConnectionString("rabbitMqSettingsPassword"));
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

builder.Services.AddAutoMapper(typeof(MovieProfile).Assembly);
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<ICacheRedis, CacheRedis>();
builder.Services.AddMediatR(Assembly.Load("Galaxi.Query.Movie.Domain"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

