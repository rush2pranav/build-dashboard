namespace BuildDashboard.Core.Models
{
    public class BuildJob
    {
        public int Id { get; set; }
        public string BuildNumber { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Branch { get; set; } = "main";
        public string Status { get; set; } = "Queued";
        public string TriggerType { get; set; } = "Manual";
        public string? TriggerBy { get; set; }
        public string? CommitHash { get; set; }
        public string? CommitMessage { get; set; }
        public DateTime QueuedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public double? DurationSeconds { get; set; }
        public string? ErrorMessage { get; set; }
        public string? LogOutput { get; set; }
        public List<BuildStep> Steps { get; set; } = new();
    }

    public class BuildStep
    {
        public int Id { get; set; }
        public int BuildJobId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public int StepOrder { get; set; }
        public string Status { get; set; } = "Pending"; 
        public double? DurationSeconds { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? LogOutput { get; set; }
    }

    public class DashboardSummary
    {
        public int TotalBuilds { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int RunningCount { get; set; }
        public int QueuedCount { get; set; }
        public double SuccessRate { get; set; }
        public double AvgDurationSeconds { get; set; }
        public double? LastBuildDuration { get; set; }
        public string? LastBuildStatus { get; set; }
        public DateTime? LastBuildTime { get; set; }
        public List<BuildTrend> DailyTrends { get; set; } = new();
        public Dictionary<string, int> BuildsByProject { get; set; } = new();
    }
    public class BuildTrend
    {
        public DateTime Date { get; set; }
        public int TotalBuilds { get; set; }
        public int Successes { get; set; }
        public int Failures { get; set; }
        public double AvgDurationSeconds { get; set; }
    }
}