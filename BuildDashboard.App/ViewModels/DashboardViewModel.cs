using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using BuildDashboard.App.Commands;
using BuildDashboard.App.Services;
using BuildDashboard.Core.Models;

namespace BuildDashboard.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _api;
        private readonly DispatcherTimer _refreshTimer;

        public DashboardViewModel()
        {
            _api = new ApiClient();
            Builds = new ObservableCollection<BuildJob>();
            Projects = new ObservableCollection<string> { "All", "GameClient", "GameServer", "AssetPipeline", "ToolsUI", "TestSuite" };

            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
            TriggerBuildCommand = new RelayCommand(async _ => await TriggerBuildAsync());

            SelectedProject = "All";
            StatusText = "Connecting to API...";

            // Auto refresh every 5 seconds
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();

            // Initial load
            _ = InitializeAsync();
        }

        // commands
        public ICommand RefreshCommand { get; }
        public ICommand TriggerBuildCommand { get; }

        // collections
        public ObservableCollection<BuildJob> Builds { get; }
        public ObservableCollection<string> Projects { get; }

        // summary properties
        private int _totalBuilds;
        public int TotalBuilds { get => _totalBuilds; set { _totalBuilds = value; OnPropertyChanged(); } }

        private int _successCount;
        public int SuccessCount { get => _successCount; set { _successCount = value; OnPropertyChanged(); } }

        private int _failedCount;
        public int FailedCount { get => _failedCount; set { _failedCount = value; OnPropertyChanged(); } }

        private int _runningCount;
        public int RunningCount { get => _runningCount; set { _runningCount = value; OnPropertyChanged(); } }

        private double _successRate;
        public double SuccessRate { get => _successRate; set { _successRate = value; OnPropertyChanged(); } }

        private double _avgDuration;
        public double AvgDuration { get => _avgDuration; set { _avgDuration = value; OnPropertyChanged(); } }

        private string? _lastBuildStatus;
        public string? LastBuildStatus { get => _lastBuildStatus; set { _lastBuildStatus = value; OnPropertyChanged(); } }

        // filter properties
        private string _selectedProject = "All";
        public string SelectedProject
        {
            get => _selectedProject;
            set { _selectedProject = value; OnPropertyChanged(); _ = LoadBuildsAsync(); }
        }

        // ui state
        private string _statusText = string.Empty;
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        private bool _isConnected;
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); } }

        private BuildJob? _selectedBuild;
        public BuildJob? SelectedBuild
        {
            get => _selectedBuild;
            set { _selectedBuild = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedBuild)); }
        }

        public bool HasSelectedBuild => SelectedBuild != null;

        // data
        private List<BuildTrend> _trends = new();
        public List<BuildTrend> Trends { get => _trends; set { _trends = value; OnPropertyChanged(); } }

        // methds
        private async Task InitializeAsync()
        {
            IsConnected = await _api.IsHealthyAsync();
            if (IsConnected)
            {
                await LoadDataAsync();
                _refreshTimer.Start();
                StatusText = "Connected — auto-refreshing every 5s";
            }
            else
            {
                StatusText = "Cannot connect to API. Make sure BuildDashboard.Api is running on localhost:5050";
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var summary = await _api.GetSummaryAsync();
                if (summary == null) return;

                TotalBuilds = summary.TotalBuilds;
                SuccessCount = summary.SuccessCount;
                FailedCount = summary.FailedCount;
                RunningCount = summary.RunningCount;
                SuccessRate = summary.SuccessRate;
                AvgDuration = summary.AvgDurationSeconds;
                LastBuildStatus = summary.LastBuildStatus;
                Trends = summary.DailyTrends;

                await LoadBuildsAsync();
                IsConnected = true;
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                IsConnected = false;
            }
        }
        private async Task LoadBuildsAsync()
        {
            try
            {
                var project = SelectedProject == "All" ? null : SelectedProject;
                var builds = await _api.GetBuildsAsync(pageSize: 50, project: project);

                Builds.Clear();
                foreach (var build in builds)
                    Builds.Add(build);
            }
            catch { }
        }
        private async Task TriggerBuildAsync()
        {
            try
            {
                var project = SelectedProject == "All" ? "GameClient" : SelectedProject;
                StatusText = $"Triggering build for {project}...";
                await _api.TriggerBuildAsync(project);
                StatusText = $"Build triggered for {project}!";
                await Task.Delay(1000);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to trigger build: {ex.Message}";
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
