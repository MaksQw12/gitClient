using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using gitclient.Services;
using gitclient.ViewModels;
using gitclient.Views.Pages;
using System.Linq;

namespace gitclient;

public partial class StartPage : UserControl
{
    private readonly RecentRepositoriesService _recent = new();

    public StartPage()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        LoadRecent();
    }

    private void LoadRecent()
    {
        var list = _recent.Load();
        RecentList.ItemsSource = list;
        NoRecentText.IsVisible = list.Count == 0;
    }

    private void OpenRecent(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
            OpenRepository(path);
    }

    private async void SelectRepo(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        var window = this.VisualRoot as Window;
        var path = await dialog.ShowAsync(window);
        if (path != null)
            OpenRepository(path);
    }

    private async void CloneRepo(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        var dialog = new gitclient.Views.CloneDialog();
        await dialog.ShowDialog(window!);

        if (dialog.ClonedPath != null)
            OpenRepository(dialog.ClonedPath);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
            SetDropZoneActive(true);
        }
        else e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e) =>
        SetDropZoneActive(false);

    private void OnDrop(object? sender, DragEventArgs e)
    {
        SetDropZoneActive(false);
        var files = e.Data.GetFiles();
        if (files != null)
            OpenRepository(files.First().Path.LocalPath);
        e.Handled = true;
    }

    private void SetDropZoneActive(bool active)
    {
        if (DropZone == null) return;
        DropZone.BorderBrush = active
            ? Brush.Parse("#7C6AF7")
            : Brush.Parse("#1E1E32");
        DropZone.Background = active
            ? Brush.Parse("#14143A")
            : Brush.Parse("#0E0E1C");
    }

    private void OpenRepository(string repoPath)
    {
        if (!GitService.IsValidRepository(repoPath))
        {
            if (DropZone != null)
            {
                DropZone.BorderBrush = Avalonia.Media.Brush.Parse("#FF6B6B");
                DropZone.Background = Avalonia.Media.Brush.Parse("#1A0E0E");

                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => SetDropZoneActive(false)));
            }
            return;
        }

        _recent.Add(repoPath);
        LoadRecent();

        var mainVM = (MainWindowViewModel?)((Window)this.VisualRoot!).DataContext;
        if (mainVM != null)
            mainVM.CurrentPage = new RepositoryPage(repoPath);
    }
}