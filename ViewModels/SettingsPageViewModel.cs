using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gitclient.Services;
using System.Diagnostics;

namespace gitclient.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly UserControl _view;
    private readonly SettingsService _settings = SettingsService.Instance;

    [ObservableProperty] private string _activeTab = "profile";
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _userEmail = "";
    [ObservableProperty] private string _gitHubToken = "";
    [ObservableProperty] private string _defaultBranchName = "main";
    [ObservableProperty] private bool _autoFetchEnabled;
    [ObservableProperty] private decimal _autoFetchInterval = 10;
    [ObservableProperty] private bool _fetchOnOpen;
    [ObservableProperty] private decimal _commitLoadLimit = 200;
    [ObservableProperty] private bool _isGitHubConnected;
    [ObservableProperty] private string _gitHubUserInfo = "";
    [ObservableProperty] private string _saveStatus = "";

    public bool IsProfileTab => ActiveTab == "profile";
    public bool IsGitTab => ActiveTab == "git";
    public bool IsIntegrationsTab => ActiveTab == "integrations";
    public bool IsPerformanceTab => ActiveTab == "performance";

    public SettingsPageViewModel(UserControl view)
    {
        _view = view;
        var s = _settings.Current;
        UserName = s.GitUserName;
        UserEmail = s.GitUserEmail;
        GitHubToken = s.GitHubToken;
        AutoFetchEnabled = s.AutoFetchEnabled;
        AutoFetchInterval = s.AutoFetchIntervalMinutes;
        FetchOnOpen = s.FetchOnOpen;
        CommitLoadLimit = s.CommitLoadLimit;
    }

    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsProfileTab));
        OnPropertyChanged(nameof(IsGitTab));
        OnPropertyChanged(nameof(IsIntegrationsTab));
        OnPropertyChanged(nameof(IsPerformanceTab));
    }

    [RelayCommand]
    private void SelectTab(string tab) => ActiveTab = tab;

    [RelayCommand]
    private void Save()
    {
        var s = _settings.Current;
        s.GitUserName = UserName;
        s.GitUserEmail = UserEmail;
        s.GitHubToken = GitHubToken;
        s.AutoFetchEnabled = AutoFetchEnabled;
        s.AutoFetchIntervalMinutes = (int)AutoFetchInterval;
        s.FetchOnOpen = FetchOnOpen;
        s.CommitLoadLimit = (int)CommitLoadLimit;
        _settings.Save();

        RunGitConfig("user.name", UserName);
        RunGitConfig("user.email", UserEmail);

        ToastService.Instance.Success("Settings saved");
        SaveStatus = "Saved";
    }

    private static void RunGitConfig(string key, string value)
    {
        try
        {
            var psi = new ProcessStartInfo("git", $"config --global {key} \"{value}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch { }
    }

    [RelayCommand]
    private void GoBack()
    {
        var mainVM = (MainWindowViewModel?)TopLevel.GetTopLevel(_view)?.DataContext;
        if (mainVM != null)
            mainVM.CurrentPage = mainVM.PreviousPage;
    }


    [RelayCommand]
    private void OpenGitHubTokenPage()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/settings/tokens/new?scopes=repo",
            UseShellExecute = true
        });
    }
}