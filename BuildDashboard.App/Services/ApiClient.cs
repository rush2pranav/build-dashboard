using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using BuildDashboard.Core.Models;
using Newtonsoft.Json;

namespace BuildDashboard.App.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public ApiClient(string baseUrl = "http://localhost:5050")
        {
            _baseUrl = baseUrl;
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<DashboardSummary?> GetSummaryAsync()
        {
            var json = await _http.GetStringAsync("/api/dashboard/summary");
            return JsonConvert.DeserializeObject<DashboardSummary>(json);
        }

        public async Task<List<BuildJob>> GetBuildsAsync(int page = 1, int pageSize = 20,
            string? project = null, string? status = null)
        {
            var url = $"/api/builds?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(project)) url += $"&project={project}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={status}";

            var json = await _http.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<BuildListResponse>(json);
            return result?.Data ?? new List<BuildJob>();
        }

        public async Task<BuildJob?> GetBuildAsync(int id)
        {
            var json = await _http.GetStringAsync($"/api/builds/{id}");
            return JsonConvert.DeserializeObject<BuildJob>(json);
        }

        public async Task<string> TriggerBuildAsync(string project)
        {
            var response = await _http.PostAsync($"/api/builds/trigger?project={project}", null);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private class BuildListResponse
        {
            [JsonProperty("data")]
            public List<BuildJob> Data { get; set; } = new();
            [JsonProperty("total")]
            public int Total { get; set; }
        }
    }
}
