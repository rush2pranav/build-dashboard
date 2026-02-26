using Microsoft.EntityFrameworkCore;
using BuildDashboard.Core.Models;

namespace BuildDashboard.Core.Data
{
    public class BuildDbContext : DbContext
    {
        public DbSet<BuildJob> BuildJobs => Set<BuildJob>();
        public DbSet<BuildStep> BuildSteps => Set<BuildStep>();

        public string DbPath { get; }

        public BuildDbContext()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BuildDashboard");
            Directory.CreateDirectory(appData);
            DbPath = Path.Combine(appData, "builds.db");
        }

        public BuildDbContext(DbContextOptions<BuildDbContext> options) : base(options)
        {
            DbPath = string.Empty;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
                options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BuildJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProjectName);
                entity.HasIndex(e => e.QueuedAtUtc);
                entity.HasMany(e => e.Steps).WithOne()
                    .HasForeignKey(s => s.BuildJobId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BuildStep>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
        public async Task SeedSampleDataAsync()
        {
            if (await BuildJobs.AnyAsync()) return;

            var random = new Random(42);
            var projects = new[] { "GameClient", "GameServer", "AssetPipeline", "ToolsUI", "TestSuite" };
            var branches = new[] { "main", "develop", "feature/ui-rework", "fix/crash-on-load", "release/v2.1" };
            var triggers = new[] { "Push", "PullRequest", "Manual", "Scheduled" };
            var users = new[] { "alice", "bob", "charlie", "diana", "eve" };
            var steps = new[] { "Restore Packages", "Build Solution", "Run Unit Tests", "Run Integration Tests", "Package Artifacts", "Deploy to Staging" };

            var commitMessages = new[]
            {
                "Fix null reference in player controller",
                "Add new particle system for explosions",
                "Refactor inventory UI to use MVVM",
                "Update weapon balance config values",
                "Optimize texture loading for large maps",
                "Fix memory leak in audio manager",
                "Add unit tests for matchmaking service",
                "Update dependencies to latest versions",
                "Implement new achievement tracking system",
                "Fix race condition in network sync",
                "Add dark mode to tools dashboard",
                "Refactor database queries for performance",
                "Fix crash when opening large asset files",
                "Add CI pipeline for automated testing",
                "Update shader compiler for DX12 support",
            };

            var buildId = 1;
            for (int daysAgo = 60; daysAgo >= 0; daysAgo--)
            {
                int buildsToday = random.Next(2, 8);
                for (int b = 0; b < buildsToday; b++)
                {
                    var queuedAt = DateTime.UtcNow.AddDays(-daysAgo)
                        .AddHours(random.Next(8, 20))
                        .AddMinutes(random.Next(0, 60));
                    var roll = random.Next(100);
                    var status = roll < 75 ? "Success" : roll < 93 ? "Failed" : "Cancelled";

                    var durationBase = random.Next(45, 300);
                    var duration = status == "Failed" ? random.Next(10, durationBase) : durationBase;

                    var project = projects[random.Next(projects.Length)];

                    var job = new BuildJob
                    {
                        BuildNumber = $"#{buildId:D4}",
                        ProjectName = project,
                        Branch = branches[random.Next(branches.Length)],
                        Status = status,
                        TriggerType = triggers[random.Next(triggers.Length)],
                        TriggerBy = users[random.Next(users.Length)],
                        CommitHash = Guid.NewGuid().ToString("N")[..8],
                        CommitMessage = commitMessages[random.Next(commitMessages.Length)],
                        QueuedAtUtc = queuedAt,
                        StartedAtUtc = queuedAt.AddSeconds(random.Next(2, 15)),
                        CompletedAtUtc = queuedAt.AddSeconds(duration),
                        DurationSeconds = duration,
                        ErrorMessage = status == "Failed" ? "Build step failed. See logs for details." : null,
                    };

                    double stepTime = 0;
                    bool hasFailed = false;
                    for (int s = 0; s < steps.Length; s++)
                    {
                        var stepDur = random.Next(5, duration / steps.Length + 20);
                        var stepStatus = "Success";

                        if (hasFailed)
                        {
                            stepStatus = "Skipped";
                            stepDur = 0;
                        }
                        else if (status == "Failed" && s == random.Next(1, steps.Length))
                        {
                            stepStatus = "Failed";
                            hasFailed = true;
                        }

                        job.Steps.Add(new BuildStep
                        {
                            StepName = steps[s],
                            StepOrder = s + 1,
                            Status = stepStatus,
                            DurationSeconds = stepDur,
                            StartedAtUtc = job.StartedAtUtc?.AddSeconds(stepTime),
                            CompletedAtUtc = job.StartedAtUtc?.AddSeconds(stepTime + stepDur),
                        });
                        stepTime += stepDur;
                    }

                    BuildJobs.Add(job);
                    buildId++;
                }
            }

            await SaveChangesAsync();
        }
    }
}