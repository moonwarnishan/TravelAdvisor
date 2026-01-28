var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TravelAdvisorDbContext>();
    await dbContext.Database.MigrateAsync();

    var districtSyncJob = scope.ServiceProvider.GetRequiredService<DistrictSyncJob>();
    await districtSyncJob.SyncDistrictsAsync();

    var cacheWarmingJob = scope.ServiceProvider.GetRequiredService<CacheWarmingJob>();
    await cacheWarmingJob.WarmupCacheAsync();
}

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Travel Advisor - Background Jobs"
    });

    var cacheWarmingSchedule = builder.Configuration.GetValue<string>("CacheSettings:CacheWarmingCronSchedule") ?? "*/15 * * * *";
    var districtSyncSchedule = builder.Configuration.GetValue<string>("CacheSettings:DistrictSyncCronSchedule") ?? "0 0 1 * *";

    RecurringJob.AddOrUpdate<CacheWarmingJob>(
        "cache-warmup",
        job => job.WarmupCacheAsync(),
        cacheWarmingSchedule,
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka")
        });

    RecurringJob.AddOrUpdate<DistrictSyncJob>(
        "district-sync",
        job => job.SyncDistrictsAsync(),
        districtSyncSchedule,
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka")
        });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Travel Advisor API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
