using System;
using System.IO;
using System.Text.Json;

namespace gitclient.Services;

public class AppSettings
{
    public string GitUserName { get; set; } = "";
    public string GitUserEmail { get; set; } = "";
    public string GitHubToken { get; set; } = "";
    public int CommitLoadLimit { get; set; } = 200;
    public bool AutoFetchEnabled { get; set; } = false;
    public int AutoFetchIntervalMinutes { get; set; } = 10;
    public bool FetchOnOpen { get; set; } = false;
}

public class SettingsService
{
    public static readonly SettingsService Instance = new();

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kommit", "settings.json");

    public AppSettings Current { get; private set; } = new();

    private SettingsService() => Load();

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) { LoadFromGit(); return; }
            var json = File.ReadAllText(SettingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
        }
        catch { Current = new(); LoadFromGit(); }
    }

    private void LoadFromGit()
    {
        Current.GitUserName = ReadGitConfig("user.name");
        Current.GitUserEmail = ReadGitConfig("user.email");
    }

    private static string ReadGitConfig(string key)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", $"config --global {key}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            return p.StandardOutput.ReadLine()?.Trim() ?? "";
        }
        catch { return ""; }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}