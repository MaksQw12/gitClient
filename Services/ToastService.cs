using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace gitclient.Services;

public enum ToastType { Success, Error, Info }

public class Toast
{
    public string Message { get; set; } = "";
    public ToastType Type { get; set; }
    public string Background => Type switch
    {
        ToastType.Success => "#1A3A2A",
        ToastType.Error => "#3A1A1A",
        _ => "#1A1A3A"
    };
    public string BorderColor => Type switch
    {
        ToastType.Success => "#2A6A4A",
        ToastType.Error => "#6A2A2A",
        _ => "#2A2A6A"
    };
    public string IconPath => Type switch
    {
        ToastType.Success => "avares://gitclient/Assets/icons/check.svg",
        ToastType.Error => "avares://gitclient/Assets/icons/error.svg",
        _ => "avares://gitclient/Assets/icons/settings.svg"
    };
}

public class ToastService
{
    public static readonly ToastService Instance = new();
    public ObservableCollection<Toast> Toasts { get; } = new();

    public void Show(string message, ToastType type = ToastType.Info)
    {
        var toast = new Toast { Message = message, Type = type };
        Dispatcher.UIThread.Post(async () =>
        {
            Toasts.Add(toast);
            await Task.Delay(3500);
            Toasts.Remove(toast);
        });
    }

    public void Success(string message) => Show(message, ToastType.Success);
    public void Error(string message) => Show(message, ToastType.Error);
    public void Info(string message) => Show(message, ToastType.Info);
}