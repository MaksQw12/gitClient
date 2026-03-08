using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace gitclient.Models;

public partial class GitFileChange : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isStaged;

    public IBrush StatusBrush => Status switch
    {
        "Modified" => new SolidColorBrush(Color.Parse("#F7A76C")),
        "Added" => new SolidColorBrush(Color.Parse("#5EE89A")),
        "Deleted" => new SolidColorBrush(Color.Parse("#FF6B6B")),
        "Renamed" => new SolidColorBrush(Color.Parse("#4FC8FF")),
        _ => new SolidColorBrush(Color.Parse("#888888")),
    };

    public string StatusShort => Status switch
    {
        "Modified" => "M",
        "Added" => "A",
        "Deleted" => "D",
        "Renamed" => "R",
        _ => "?",
    };

    public string StatusIcon => Status switch
    {
        "Modified" => "avares://gitclient/Assets/icons/file-modified.svg",
        "Added" => "avares://gitclient/Assets/icons/file-added.svg",
        "Deleted" => "avares://gitclient/Assets/icons/file-deleted.svg",
        _ => "avares://gitclient/Assets/icons/file-modified.svg",
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(StatusShort));
        OnPropertyChanged(nameof(StatusIcon));
    }
}