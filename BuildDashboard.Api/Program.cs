using BuildDashboard.Core.Data;
using BuildDashboard.Core.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BuildDbContext>(options =>
{
    var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BuildDashboard", "builds.db");
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// seed database w sample data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BuildDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.SeedSampleDataAsync();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

//  endpoints

app.MapGet("/api/builds", async (BuildDbContext db, string? project, string? status,
    int page = 1, int pageSize = 20) =>
{
    var query = db.BuildJobs.Include(b => b.Steps).AsQueryable();
    if (!string.IsNullOrEmpty(project)) query = query.Where(b => b.ProjectName == project);
    if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);

    var total = await query.CountAsync();
    var builds = await query.OrderByDescending(b => b.QueuedAtUtc)
        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { total, page, pageSize, data = builds });
}).WithTags("Builds");

app.MapGet("/api/builds/{id}", async (int id, BuildDbContext db) =>
{
    var build = await db.BuildJobs.Include(b => b.Steps).FirstOrDefaultAsync(b => b.Id == id);
    return build is null ? Results.NotFound() : Results.Ok(build);
}).WithTags("Builds");

app.MapPost("/api/builds/trigger", async (BuildDbContext db, string? project) =>
{
    var buildCount = await db.BuildJobs.CountAsync();
    var job = new BuildJob
    {
        BuildNumber = $"#{buildCount + 1:D4}",
        ProjectName = project ?? "GameClient",
        Branch = "main",
        Status = "Queued",
        TriggerType = "Manual",
        TriggerBy = "dashboard-user",
        CommitHash = Guid.NewGuid().ToString("N")[..8],
        CommitMessage = "Manual build triggered from dashboard",
        QueuedAtUtc = DateTime.UtcNow,
    };
    job.Steps.AddRange(new[]
    {
        new BuildStep { StepName = "Restore Packages", StepOrder = 1, Status = "Pending" },
        new BuildStep { StepName = "Build Solution", StepOrder = 2, Status = "Pending" },
        new BuildStep { StepName = "Run Unit Tests", StepOrder = 3, Status = "Pending" },
        new BuildStep { StepName = "Package Artifacts", StepOrder = 4, Status = "Pending" },
    });

    db.BuildJobs.Add(job);
    await db.SaveChangesAsync();

    // simulating the build execution in the background
    _ = Task.Run(async () =>
    {
        using var simDb = new BuildDbContext();
        var simJob = await simDb.BuildJobs.Include(b => b.Steps).FirstAsync(b => b.Id == job.Id);
        simJob.Status = "Running";
        simJob.StartedAtUtc = DateTime.UtcNow;
        await simDb.SaveChangesAsync();

        var random = new Random();
        foreach (var step in simJob.Steps.OrderBy(s => s.StepOrder))
        {
            step.Status = "Running";
            step.StartedAtUtc = DateTime.UtcNow;
            await simDb.SaveChangesAsync();

            await Task.Delay(random.Next(2000, 5000)); //work simulation

            var success = random.Next(100) < 90; // success rate indicator
            step.Status = success ? "Success" : "Failed";
            step.CompletedAtUtc = DateTime.UtcNow;
            step.DurationSeconds = (step.CompletedAtUtc.Value - step.StartedAtUtc.Value).TotalSeconds;
            await simDb.SaveChangesAsync();

            if (!success)
            {
                // Fail remaining steps
                foreach (var remaining in simJob.Steps.Where(s => s.StepOrder > step.StepOrder))
                    remaining.Status = "Skipped";
                simJob.Status = "Failed";
                simJob.ErrorMessage = $"Step '{step.StepName}' failed";
                break;
            }
        }

        if (simJob.Status == "Running") simJob.Status = "Success";
        simJob.CompletedAtUtc = DateTime.UtcNow;
        simJob.DurationSeconds = (simJob.CompletedAtUtc.Value - simJob.StartedAtUtc!.Value).TotalSeconds;
        await simDb.SaveChangesAsync();
    });

    return Results.Ok(new { message = "Build triggered", buildId = job.Id, buildNumber = job.BuildNumber });
}).WithTags("Builds");

app.MapGet("/api/dashboard/summary", async (BuildDbContext db) =>
{
    var builds = await db.BuildJobs.ToListAsync();
    var completed = builds.Where(b => b.Status is "Success" or "Failed").ToList();

    var summary = new DashboardSummary
    {
        TotalBuilds = builds.Count,
        SuccessCount = builds.Count(b => b.Status == "Success"),
        FailedCount = builds.Count(b => b.Status == "Failed"),
        RunningCount = builds.Count(b => b.Status == "Running"),
        QueuedCount = builds.Count(b => b.Status == "Queued"),
        SuccessRate = completed.Any() ? Math.Round(completed.Count(b => b.Status == "Success") * 100.0 / completed.Count, 1) : 0,
        AvgDurationSeconds = completed.Any() ? Math.Round(completed.Where(b => b.DurationSeconds.HasValue).Average(b => b.DurationSeconds!.Value), 1) : 0,
        BuildsByProject = builds.GroupBy(b => b.ProjectName).ToDictionary(g => g.Key, g => g.Count()),
    };

    var lastBuild = builds.OrderByDescending(b => b.QueuedAtUtc).FirstOrDefault();
    if (lastBuild != null)
    {
        summary.LastBuildStatus = lastBuild.Status;
        summary.LastBuildTime = lastBuild.QueuedAtUtc;
        summary.LastBuildDuration = lastBuild.DurationSeconds;
    }

    summary.DailyTrends = builds
        .Where(b => b.QueuedAtUtc >= DateTime.UtcNow.AddDays(-30))
        .GroupBy(b => b.QueuedAtUtc.Date)
        .OrderBy(g => g.Key)
        .Select(g => new BuildTrend
        {
            Date = g.Key,
            TotalBuilds = g.Count(),
            Successes = g.Count(b => b.Status == "Success"),
            Failures = g.Count(b => b.Status == "Failed"),
            AvgDurationSeconds = g.Where(b => b.DurationSeconds.HasValue).Any()
                ? Math.Round(g.Where(b => b.DurationSeconds.HasValue).Average(b => b.DurationSeconds!.Value), 1) : 0
        }).ToList();

    return Results.Ok(summary);
}).WithTags("Dashboard");

app.MapGet("/api/dashboard/projects", async (BuildDbContext db) =>
{
    var projects = await db.BuildJobs
        .GroupBy(b => b.ProjectName)
        .Select(g => new
        {
            project = g.Key,
            totalBuilds = g.Count(),
            successRate = g.Count(b => b.Status == "Success") * 100.0 / g.Count(b => b.Status == "Success" || b.Status == "Failed"),
            lastBuild = g.Max(b => b.QueuedAtUtc),
            avgDuration = g.Where(b => b.DurationSeconds.HasValue).Average(b => b.DurationSeconds)
        }).ToListAsync();

    return Results.Ok(projects);
}).WithTags("Dashboard");

Console.WriteLine("\n  Build Dashboard API - http://localhost:5050/swagger\n");
app.Run("http://localhost:5050");