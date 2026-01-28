using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TravelAdvisor.Application.Common.Interfaces;

namespace TravelAdvisor.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly bool _isConnected;

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Redis connection string not configured. Caching disabled");
            _isConnected = false;
            return;
        }

        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;

            _redis = ConnectionMultiplexer.Connect(options);
            _database = _redis.GetDatabase();
            _isConnected = _redis.IsConnected;

            if (_isConnected)
            {
                _logger.LogInformation("Successfully connected to Redis");
            }
            else
            {
                _logger.LogWarning("Failed to connect to Redis. Caching disabled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Redis connection. Caching disabled");
            _isConnected = false;
        }
    }

    public bool IsAvailable => _isConnected && _redis?.IsConnected == true;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!IsAvailable || _database is null)
            return null;

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get value from Redis for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!IsAvailable || _database is null)
            return;

        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serialized, expiration, When.Always);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set value in Redis for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _database is null)
            return;

        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove value from Redis for key: {Key}", key);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
