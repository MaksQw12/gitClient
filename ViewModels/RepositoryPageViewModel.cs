using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gitclient.Models;
using gitclient.Services;
using LibGit2Sharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gitclient.ViewModels;

public partial class BranchItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editName = "";
    public string Name { get; }
    public bool IsRemote => Name.StartsWith("origin/") || Name.StartsWith("remotes/");
    public BranchItemViewModel(string name) => Name = name;
}

public partial class RepositoryPageViewModel : ViewModelBase
{
    private readonly GitService _git;
    private readonly string _repoPath;
    private readonly UserControl _view;
    private FileSystemWatcher? _watcher;
    private GitCommit? _savedSelection;

    public ObservableCollection<GitCommit> Commits { get; } = new();
    public ObservableCollection<GitFileChange> ChangedFiles { get; } = new();
    public ObservableCollection<DiffLine> DiffLines { get; } = new();
    public ObservableCollection<BranchItemViewModel> Branches { get; } = new();

    [ObservableProperty] private GitCommit? _selectedCommit;
    [ObservableProperty] private GitFileChange? _selectedFile;
    [ObservableProperty] private string _currentBranch = "";
    [ObservableProperty] private string _commitMessage = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isBranchPopupOpen;
    [ObservableProperty] private bool _isCreatingBranch;
    [ObservableProperty] private string _newBranchName = "";
    [ObservableProperty] private string _selectedFileAuthor = "";
    [ObservableProperty] private int _aheadBy;
    [ObservableProperty] private int _behindBy;
    [ObservableProperty] private bool _hasTracking;
    [ObservableProperty] private string _stashMessage = "";
    [ObservableProperty] private bool _isStashPopupOpen;
    public ObservableCollection<StashItem> Stashes { get; } = new();

    public bool IsCommitMode => SelectedCommit != null;
    public bool IsWorkingMode => SelectedCommit == null;

    public RepositoryPageViewModel(string repoPath, UserControl view)
    {
        _repoPath = repoPath;
        _view = view;
        _git = new GitService();
        _ = LoadAsync();
        StartWatcher();
    }

    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_repoPath)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        System.Timers.Timer? debounce = null;

        void OnChanged(object s, FileSystemEventArgs e)
        {
            var rel = Path.GetRelativePath(_repoPath, e.FullPath);
            if (rel.StartsWith(".git") &&
                !rel.Equals(".git\\HEAD") && !rel.Equals(".git/HEAD") &&
                !rel.Equals(".git\\index") && !rel.Equals(".git/index"))
                return;

            debounce?.Stop();
            debounce?.Dispose();
            debounce = new System.Timers.Timer(500) { AutoReset = false };
            debounce.Elapsed += async (_, _) =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (SelectedCommit == null)
                        await RefreshWorkingFilesAsync();
                });
            };
            debounce.Start();
        }

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += (s, e) => OnChanged(s, e);
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var branch = await Task.Run(() => _git.GetCurrentBranch(_repoPath));
            var branches = await Task.Run(() => _git.GetBranches(_repoPath));
            var commits = await Task.Run(() => _git.GetCommits(_repoPath));

            CurrentBranch = branch;

            var aheadBehind = await Task.Run(() => _git.GetAheadBehind(_repoPath));
            AheadBy = aheadBehind.ahead;
            BehindBy = aheadBehind.behind;
            HasTracking = aheadBehind.ahead > 0 || aheadBehind.behind > 0;

            var stashes = await Task.Run(() => _git.GetStashes(_repoPath));
            Stashes.Clear();
            foreach (var s in stashes) Stashes.Add(s);

            Branches.Clear();
            foreach (var b in branches) Branches.Add(new BranchItemViewModel(b));

            Commits.Clear();
            foreach (var c in commits) Commits.Add(c);

            if (SelectedCommit == null)
                await RefreshWorkingFilesAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task RefreshWorkingFilesAsync()
    {
        var files = await Task.Run(() => _git.GetChangedFiles(_repoPath));
        var newPaths = files.Select(f => f.FilePath).ToHashSet();
        var oldPaths = ChangedFiles.Select(f => f.FilePath).ToHashSet();

        for (int i = ChangedFiles.Count - 1; i >= 0; i--)
            if (!newPaths.Contains(ChangedFiles[i].FilePath))
                ChangedFiles.RemoveAt(i);

        foreach (var f in files)
        {
            var existing = ChangedFiles.FirstOrDefault(x => x.FilePath == f.FilePath);
            if (existing == null)
            {
                ChangedFiles.Add(f);
            }
            else
            {
                existing.Status = f.Status;
                existing.IsStaged = f.IsStaged;
            }
        }
    }

    partial void OnSelectedCommitChanged(GitCommit? value)
    {
        SelectedFile = null;
        DiffLines.Clear();
        SelectedFileAuthor = "";
        OnPropertyChanged(nameof(IsCommitMode));
        OnPropertyChanged(nameof(IsWorkingMode));
        ChangedFiles.Clear();

        if (value == null)
        {
            _ = RefreshWorkingFilesAsync();
            return;
        }

        _ = Task.Run(() => _git.GetCommitFiles(_repoPath, value.Sha))
            .ContinueWith(t =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var f in t.Result) ChangedFiles.Add(f);
                });
            });
    }

    partial void OnSelectedFileChanged(GitFileChange? value)
    {
        DiffLines.Clear();
        SelectedFileAuthor = "";
        if (value == null) return;

        var sha = SelectedCommit?.Sha;
        var filePath = value.FilePath;

        _ = Task.Run(() =>
        {
            try
            {
                using var repo = new LibGit2Sharp.Repository(_repoPath);
                if (sha != null)
                {
                    var commit = repo.Lookup<LibGit2Sharp.Commit>(sha);
                    return commit != null
                        ? $"{commit.Author.Name}  ·  {commit.Author.When:MMM dd, yyyy  HH:mm}"
                        : "";
                }
                else
                {
                    var lastCommit = repo.Commits
                        .QueryBy(filePath)
                        .FirstOrDefault();
                    return lastCommit != null
                        ? $"Last: {lastCommit.Commit.Author.Name}  ·  {lastCommit.Commit.Author.When:MMM dd, yyyy  HH:mm}"
                        : "";
                }
            }
            catch { return ""; }
        }).ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() => SelectedFileAuthor = t.Result);
        });

        _ = Task.Run(() => sha != null
                ? _git.GetFileDiff(_repoPath, sha, filePath)
                : _git.GetWorkingDiff(_repoPath, filePath))
            .ContinueWith(t =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    DiffLines.Clear();
                    foreach (var line in t.Result.Split('\n'))
                    {
                        var type = line switch
                        {
                            var l when l.StartsWith("+++") || l.StartsWith("---") => DiffLineType.Meta,
                            var l when l.StartsWith("@@") => DiffLineType.Header,
                            var l when l.StartsWith("+") => DiffLineType.Added,
                            var l when l.StartsWith("-") => DiffLineType.Removed,
                            var l when l.StartsWith("diff") || l.StartsWith("index") => DiffLineType.Meta,
                            _ => DiffLineType.Normal,
                        };
                        DiffLines.Add(new DiffLine { Text = line, Type = type });
                    }
                });
            });
    }

    [RelayCommand]
    private void ToggleBranchPopup() => IsBranchPopupOpen = !IsBranchPopupOpen;

    [RelayCommand]
    private async Task SelectBranch(BranchItemViewModel item)
    {
        if (item.Name == CurrentBranch) { IsBranchPopupOpen = false; return; }
        IsBusy = true;
        IsBranchPopupOpen = false;
        try
        {
            await Task.Run(() =>
            {
                using var repo = new LibGit2Sharp.Repository(_repoPath);

                if (item.IsRemote)
                {
                    var localName = item.Name.Substring(item.Name.IndexOf('/') + 1);

                    var localBranch = repo.Branches[localName];
                    if (localBranch == null)
                    {
                        var remoteBranch = repo.Branches[item.Name];
                        localBranch = repo.CreateBranch(localName, remoteBranch.Tip);
                        repo.Branches.Update(localBranch,
                            b => b.TrackedBranch = remoteBranch.CanonicalName);
                    }
                    LibGit2Sharp.Commands.Checkout(repo, localBranch);
                }
                else
                {
                    var branch = repo.Branches[item.Name];
                    if (branch != null)
                        LibGit2Sharp.Commands.Checkout(repo, branch);
                }
            });
            SelectedCommit = null;
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Checkout failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task StageFile(GitFileChange file)
    {
        await Task.Run(() => _git.StageFile(_repoPath, file.FilePath));
        await RefreshWorkingFilesAsync();
    }

    [RelayCommand]
    private async Task UnstageFile(GitFileChange file)
    {
        await Task.Run(() => _git.UnstageFile(_repoPath, file.FilePath));
        await RefreshWorkingFilesAsync();
    }

    [RelayCommand]
    private async Task StageAll()
    {
        await Task.Run(() => _git.StageFile(_repoPath, "*"));
        await RefreshWorkingFilesAsync();
    }

    [RelayCommand]
    private void ClearCommitSelection() => SelectedCommit = null;

    [RelayCommand]
    private async Task CopyHash(string sha)
    {
        var clipboard = TopLevel.GetTopLevel(_view)?.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(sha);
    }

    [RelayCommand]
    private async Task CopyShortHash(string sha)
    {
        var clipboard = TopLevel.GetTopLevel(_view)?.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(sha);
    }

    [RelayCommand]
    private async Task CopyMessage(string message)
    {
        var clipboard = TopLevel.GetTopLevel(_view)?.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(message);
    }

    [RelayCommand]
    private async Task Fetch()
    {
        IsBusy = true;
        try
        {
            await Task.Run(() => _git.Fetch(_repoPath));
            ToastService.Instance.Success("Fetched successfully");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Fetch failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Pull()
    {
        IsBusy = true;
        try
        {
            var settings = SettingsService.Instance.Current;
            await Task.Run(() => _git.Pull(_repoPath, settings.GitUserName, settings.GitUserEmail));
            ToastService.Instance.Success("Pulled successfully");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Pull failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Push()
    {
        IsBusy = true;
        try
        {
            await Task.Run(() => _git.Push(_repoPath));
            ToastService.Instance.Success("Pushed successfully");
        }
        catch (Exception ex) { ToastService.Instance.Error($"Push failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Commit()
    {
        if (string.IsNullOrWhiteSpace(CommitMessage)) return;

        var hasStagedFiles = ChangedFiles.Any(f => f.IsStaged);
        if (!hasStagedFiles)
        {
            ToastService.Instance.Error("Stage files before committing");
            return;
        }

        IsBusy = true;
        try
        {
            var msg = CommitMessage;
            var settings = SettingsService.Instance.Current;
            await Task.Run(() => _git.CommitChanges(_repoPath, msg, settings.GitUserName, settings.GitUserEmail));
            CommitMessage = "";
            ToastService.Instance.Success("Committed");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CommitAndPush()
    {
        if (string.IsNullOrWhiteSpace(CommitMessage)) return;

        var hasStagedFiles = ChangedFiles.Any(f => f.IsStaged);
        if (!hasStagedFiles)
        {
            ToastService.Instance.Error("Stage files before committing");
            return;
        }

        IsBusy = true;
        try
        {
            var msg = CommitMessage;
            var settings = SettingsService.Instance.Current;
            await Task.Run(() =>
            {
                _git.CommitChanges(_repoPath, msg, settings.GitUserName, settings.GitUserEmail);
                _git.Push(_repoPath);
            });
            CommitMessage = "";
            ToastService.Instance.Success("Committed & pushed");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error(ex.Message); }
        finally { IsBusy = false; }
    }

    public void SaveSelection() => _savedSelection = SelectedCommit;
    public void RestoreSelection()
    {
        if (_savedSelection != SelectedCommit)
            SelectedCommit = _savedSelection;
    }

    [RelayCommand]
    private void GoBack()
    {
        var mainVM = (MainWindowViewModel?)TopLevel.GetTopLevel(_view)?.DataContext;
        if (mainVM != null)
            mainVM.CurrentPage = new gitclient.StartPage();
    }

    [RelayCommand]
    private async Task CreateBranch()
    {
        if (string.IsNullOrWhiteSpace(NewBranchName)) return;
        IsBusy = true;
        try
        {
            var name = NewBranchName;
            await Task.Run(() =>
            {
                using var repo = new LibGit2Sharp.Repository(_repoPath);
                var branch = repo.CreateBranch(name);
                LibGit2Sharp.Commands.Checkout(repo, branch);
            });
            NewBranchName = "";
            IsCreatingBranch = false;
            ToastService.Instance.Success($"Branch '{name}' created");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteBranch(BranchItemViewModel item)
    {
        if (item.Name == CurrentBranch)
        {
            ToastService.Instance.Error("Cannot delete current branch");
            return;
        }

        var dialog = new gitclient.Views.ConfirmDialog($"Delete branch '{item.Name}'? This cannot be undone.");
        var window = TopLevel.GetTopLevel(_view) as Avalonia.Controls.Window;
        if (window != null) await dialog.ShowDialog(window);
        if (!dialog.Confirmed) return;

        IsBusy = true;
        try
        {
            await Task.Run(() =>
            {
                using var repo = new LibGit2Sharp.Repository(_repoPath);
                repo.Branches.Remove(item.Name);
            });
            ToastService.Instance.Success($"Branch '{item.Name}' deleted");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Failed to delete: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DiscardFile(GitFileChange file)
    {
        await Task.Run(() =>
        {
            using var repo = new LibGit2Sharp.Repository(_repoPath);

            if (file.Status == "Added")
            {
                var fullPath = Path.Combine(_repoPath, file.FilePath);
                if (File.Exists(fullPath)) File.Delete(fullPath);

                Commands.Unstage(repo, file.FilePath);
            }
            else
            {
                if (file.IsStaged)
                    Commands.Unstage(repo, file.FilePath);

                repo.CheckoutPaths(repo.Head.Tip.Sha,
                    new[] { file.FilePath },
                    new LibGit2Sharp.CheckoutOptions
                    {
                        CheckoutModifiers = LibGit2Sharp.CheckoutModifiers.Force
                    });
            }
        });
        await RefreshWorkingFilesAsync();
    }

    [RelayCommand]
    private void ToggleCreateBranch() => IsCreatingBranch = !IsCreatingBranch;

    [RelayCommand]
    private void OpenSettings()
    {
        var mainVM = (MainWindowViewModel?)TopLevel.GetTopLevel(_view)?.DataContext;
        if (mainVM != null)
        {
            mainVM.PreviousPage = mainVM.CurrentPage;
            mainVM.CurrentPage = new gitclient.Views.Pages.SettingsPage();
        }
    }

    [RelayCommand]
    private void StartRenameBranch(BranchItemViewModel item)
    {
        if (item == null) return;
        foreach (var b in Branches) b.IsEditing = false;
        item.EditName = item.Name;
        item.IsEditing = true;
    }

    [RelayCommand]
    private async Task ConfirmRenameBranch(BranchItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(item.EditName)) return;
        var oldName = item.Name;
        var newName = item.EditName;
        item.IsEditing = false;

        IsBusy = true;
        try
        {
            await Task.Run(() =>
            {
                using var repo = new LibGit2Sharp.Repository(_repoPath);
                repo.Branches.Rename(oldName, newName);
            });
            ToastService.Instance.Success($"Renamed to '{newName}'");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Rename failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelRenameBranch(BranchItemViewModel item) => item.IsEditing = false;

    [RelayCommand]
    private async Task StashSave()
    {
        IsBusy = true;
        IsStashPopupOpen = false;
        try
        {
            var msg = string.IsNullOrWhiteSpace(StashMessage) ? "WIP" : StashMessage;
            var settings = SettingsService.Instance.Current;
            await Task.Run(() => _git.StashSave(_repoPath, msg, settings.GitUserName, settings.GitUserEmail));
            StashMessage = "";
            ToastService.Instance.Success("Stashed changes");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Stash failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task StashPop(StashItem item)
    {
        IsBusy = true;
        IsStashPopupOpen = false;
        try
        {
            await Task.Run(() => _git.StashPop(_repoPath, item.Index));
            ToastService.Instance.Success("Stash applied");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Pop failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task StashDrop(StashItem item)
    {
        IsBusy = true;
        try
        {
            await Task.Run(() => _git.StashDrop(_repoPath, item.Index));
            ToastService.Instance.Success("Stash dropped");
            await LoadAsync();
        }
        catch (Exception ex) { ToastService.Instance.Error($"Drop failed: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ToggleStashPopup() => IsStashPopupOpen = !IsStashPopupOpen;
}