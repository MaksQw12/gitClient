using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Avalonia.VisualTree;
using gitclient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gitclient.Views;

public partial class CloneDialog : Window
{
    public string? ClonedPath { get; private set; }
    private List<GitHubRepo> _repos = new();
    private bool _loadedRepos = false;

    public CloneDialog()
    {
        InitializeComponent();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (RepoDropdown.IsVisible)
        {
            var src = e.Source as Avalonia.Visual;
            var current = src;
            var inside = false;
            while (current != null)
            {
                if (current == RepoDropdown || current == UrlBox)
                {
                    inside = true;
                    break;
                }
                current = current.GetVisualParent() as Avalonia.Visual;
            }
            if (!inside) RepoDropdown.IsVisible = false;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(this);
            if (point.Y <= 44) BeginMoveDrag(e);
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private async void OnUrlFocused(object? sender, GotFocusEventArgs e)
    {
        var token = SettingsService.Instance.Current.GitHubToken;
        if (string.IsNullOrEmpty(token)) return;

        if (!_loadedRepos)
        {
            _loadedRepos = true;
            StatusText.Text = "Loading repos...";
            StatusText.Foreground = Brush.Parse("#7C6AF7");
            _repos = await GitHubService.Instance.GetReposAsync(token);
            StatusText.Text = "";
        }

        var text = UrlBox.Text?.Trim() ?? "";
        if (_repos.Count > 0 && !text.StartsWith("http") && !text.StartsWith("git@"))
            ShowRepoDropdown(string.IsNullOrEmpty(text) ? _repos :
                _repos.FindAll(r => r.FullName.ToLower().Contains(text.ToLower())));
    }

    private static Avalonia.Controls.Image MakeSvgIcon(string path)
    {
        var source = SvgSource.Load(path, null);
        return new Avalonia.Controls.Image
        {
            Source = new SvgImage { Source = source },
            Width = 11,
            Height = 11
        };
    }

    private void ShowRepoDropdown(List<GitHubRepo> repos)
    {
        RepoList.Children.Clear();
        if (repos.Count == 0) { RepoDropdown.IsVisible = false; return; }

        foreach (var repo in repos)
        {
            var btn = new Button
            {
                Background = Brush.Parse("Transparent"),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Thickness(10, 7),
                Cursor = new Cursor(StandardCursorType.Hand),
                CornerRadius = new CornerRadius(6),
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(8)));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var iconBorder = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(5),
                Background = Brush.Parse("#13122A"),
                Child = MakeSvgIcon("avares://gitclient/Assets/icons/git-branch.svg"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            Grid.SetColumn(iconBorder, 0);

            var textPanel = new StackPanel { Spacing = 2 };
            textPanel.Children.Add(new TextBlock
            {
                Text = repo.FullName,
                FontSize = 12,
                Foreground = Brush.Parse("#C0C0D8"),
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(repo.Description) ? "—" : repo.Description,
                FontSize = 10,
                Foreground = Brush.Parse("#44445A"),
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            });
            Grid.SetColumn(textPanel, 2);

            row.Children.Add(iconBorder);
            row.Children.Add(textPanel);
            btn.Content = row;

            var cloneUrl = repo.CloneUrl;
            btn.Click += (_, _) =>
            {
                UrlBox.Text = cloneUrl;
                RepoDropdown.IsVisible = false;
            };
            btn.PointerEntered += (_, _) => btn.Background = Brush.Parse("#1E1E35");
            btn.PointerExited += (_, _) => btn.Background = Brush.Parse("Transparent");

            RepoList.Children.Add(btn);
        }

        RepoDropdown.IsVisible = true;
    }

    private void OnUrlChanged(object? sender, TextChangedEventArgs e)
    {
        var query = UrlBox.Text?.Trim() ?? "";

        if (_repos.Count > 0)
        {
            if (string.IsNullOrEmpty(query))
            {
                ShowRepoDropdown(_repos);
            }
            else if (!query.StartsWith("http") && !query.StartsWith("git@"))
            {
                var filtered = _repos.FindAll(r =>
                    r.FullName.ToLower().Contains(query.ToLower()) ||
                    r.Name.ToLower().Contains(query.ToLower()));
                ShowRepoDropdown(filtered);
            }
            else
            {
                RepoDropdown.IsVisible = false;
            }
        }

        var repoName = ExtractRepoName(query);
        PreviewBorder.IsVisible = !string.IsNullOrEmpty(repoName)
                                && (query.StartsWith("http") || query.StartsWith("git@"));
        if (PreviewBorder.IsVisible)
        {
            RepoNameText.Text = repoName;
            UpdateFinalPath();
        }

        UpdateCloneButton();
    }

    private async void BrowsePath(object? sender, RoutedEventArgs e)
    {
        RepoDropdown.IsVisible = false;
        var dialog = new OpenFolderDialog { Title = "Select destination folder" };
        var path = await dialog.ShowAsync(this);
        if (path != null)
        {
            PathBox.Text = path;
            UpdateFinalPath();
            UpdateCloneButton();
        }
    }

    private void UpdateFinalPath()
    {
        var basePath = PathBox.Text?.Trim() ?? "";
        var repoName = ExtractRepoName(UrlBox.Text?.Trim() ?? "");
        FinalPathText.Text = (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(repoName))
            ? Path.Combine(basePath, repoName) : "";
    }

    private void UpdateCloneButton()
    {
        CloneBtn.IsEnabled = !string.IsNullOrEmpty(UrlBox.Text?.Trim())
                          && !string.IsNullOrEmpty(PathBox.Text?.Trim());
    }

    private async void OnClone(object? sender, RoutedEventArgs e)
    {
        RepoDropdown.IsVisible = false;
        var url = UrlBox.Text?.Trim();
        var basePath = PathBox.Text?.Trim();
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(basePath)) return;

        var finalPath = Path.Combine(basePath, ExtractRepoName(url));

        CloneBtn.IsEnabled = false;
        StatusText.Text = "Cloning...";
        StatusText.Foreground = Brush.Parse("#7C6AF7");

        try
        {
            await Task.Run(() => LibGit2Sharp.Repository.Clone(url, finalPath));
            ClonedPath = finalPath;
            StatusText.Text = "Done!";
            StatusText.Foreground = Brush.Parse("#5EE89A");
            await Task.Delay(600);
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message.Length > 60 ? ex.Message[..60] + "..." : ex.Message;
            StatusText.Foreground = Brush.Parse("#FF6B6B");
            CloneBtn.IsEnabled = true;
        }
    }

    private static string ExtractRepoName(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        var name = url.TrimEnd('/');
        var slash = name.LastIndexOf('/');
        if (slash >= 0) name = name[(slash + 1)..];
        if (name.EndsWith(".git")) name = name[..^4];
        return name;
    }
}