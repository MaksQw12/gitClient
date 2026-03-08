using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace gitclient.Services;

public class RecentRepository
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path.TrimEnd('/', '\\'));
    public DateTime LastOpened { get; set; }
}

public class RecentRepositoriesService
{
    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kommit", "recent.json");

    public List<RecentRepository> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<RecentRepository>>(json) ?? new();
        }
        catch { return new(); }
    }

    public void Add(string path)
    {
        var list = Load();
        list.RemoveAll(r => r.Path == path);
        list.Insert(0, new RecentRepository { Path = path, LastOpened = DateTime.Now });
        if (list.Count > 10) list = list.Take(10).ToList();
        Save(list);
    }

    private void Save(List<RecentRepository> list)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(list));
        }
        catch { }
    }
}