using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using gitclient.ViewModels;

namespace gitclient.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel(this);

        this.AddHandler(PointerPressedEvent, (s, e) =>
        {
            if (e.Source is not TextBox)
            {
                var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
                if (focused is TextBox tb)
                {
                    tb.IsEnabled = false;
                    tb.IsEnabled = true;
                }
            }
        }, RoutingStrategies.Tunnel);
    }
}