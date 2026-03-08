using Avalonia.Controls;

namespace gitclient.Views;

public partial class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        CancelButton.Click += (_, _) => { Confirmed = false; Close(); };
        ConfirmButton.Click += (_, _) => { Confirmed = true; Close(); };
    }
}