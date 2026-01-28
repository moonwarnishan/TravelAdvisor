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

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Travel Advisor - Background Jobs"
    });

    var cronSchedule = builder.Configuration.GetValue<string>("CacheSettings:CacheWarmingCronSchedule") ?? "*/15 * * * *";

    RecurringJob.AddOrUpdate<CacheWarmingJob>(
        "cache-warmup",
        job => job.WarmupCacheAsync(),
        cronSchedule,
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka")
        });

    BackgroundJob.Enqueue<CacheWarmingJob>(job => job.WarmupCacheAsync());
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
