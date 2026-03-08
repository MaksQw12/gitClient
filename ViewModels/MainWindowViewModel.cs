using CommunityToolkit.Mvvm.ComponentModel;
using gitclient.Services;
using System.Collections.ObjectModel;

namespace gitclient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? currentPage;
    [ObservableProperty] 
    private object? _previousPage;
    public ObservableCollection<Toast> Toasts => ToastService.Instance.Toasts;

    public MainWindowViewModel()
    {
        currentPage = new StartPage();
    }


}
