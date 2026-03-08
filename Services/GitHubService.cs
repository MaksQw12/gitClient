using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace gitclient.Services;

public class GitHubRepo
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string CloneUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Private { get; set; }
    public int StargazersCount { get; set; }
}

public class GitHubService
{
    public static readonly GitHubService Instance = new();
    private static readonly HttpClient _http = new();

    public async Task<List<GitHubRepo>> GetReposAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/user/repos?per_page=100&sort=updated&affiliation=owner");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("User-Agent", "Kommit");
        request.Headers.Add("Accept", "application/vnd.github+json");

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        var repos = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();

        var result = new List<GitHubRepo>();
        foreach (var r in repos)
        {
            result.Add(new GitHubRepo
            {
                Name = r.GetProperty("name").GetString() ?? "",
                FullName = r.GetProperty("full_name").GetString() ?? "",
                CloneUrl = r.GetProperty("clone_url").GetString() ?? "",
                Description = r.TryGetProperty("description", out var d) && d.ValueKind != JsonValueKind.Null
                                     ? d.GetString() ?? "" : "",
                Private = r.GetProperty("private").GetBoolean(),
                StargazersCount = r.GetProperty("stargazers_count").GetInt32(),
            });
        }
        return result;
    }
}