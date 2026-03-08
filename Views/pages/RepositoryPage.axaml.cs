using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Svg.Skia;
using Avalonia.VisualTree;
using gitclient.Models;
using gitclient.ViewModels;


namespace gitclient.Views.Pages;

public partial class RepositoryPage : UserControl
{
    private RepositoryPageViewModel? _vm;

    public RepositoryPage(string repoPath)
    {
        InitializeComponent();
        _vm = new RepositoryPageViewModel(repoPath, this);
        DataContext = _vm;

        var commitList = this.FindControl<ListBox>("CommitList");
        if (commitList != null)
        {
            commitList.ContainerPrepared += OnCommitItemPrepared;

            commitList.AddHandler(PointerPressedEvent, (s, e) =>
            {
                if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                {
                    _vm?.SaveSelection();
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);

            commitList.AddHandler(PointerReleasedEvent, (s, e) =>
            {
                if (e.GetCurrentPoint(null).Properties.PointerUpdateKind ==
                    PointerUpdateKind.RightButtonReleased)
                    _vm?.RestoreSelection();
            }, RoutingStrategies.Tunnel);
        }

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

        this.AttachedToVisualTree += (_, _) =>
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window == null) return;

            window.AddHandler(PointerPressedEvent, (s, e) =>
            {
                if (_vm?.IsBranchPopupOpen != true) return;

                var source = e.Source as Avalonia.Visual;
                var current = source;
                var insidePopupOrButton = false;

                while (current != null)
                {
                    if (current is Avalonia.Controls.Primitives.Popup)
                    {
                        insidePopupOrButton = true;
                        break;
                    }
                    if (current is Button btn && btn.Classes.Contains("branch"))
                    {
                        insidePopupOrButton = true;
                        break;
                    }
                    current = current.GetVisualParent() as Avalonia.Visual;
                }

                if (!insidePopupOrButton)
                    _vm.IsBranchPopupOpen = false;

            }, RoutingStrategies.Bubble);
        };
    }

    private static Avalonia.Controls.Image MakeSvgIcon(string path)
    {
        var source = SvgSource.Load(path, null);
        return new Avalonia.Controls.Image
        {
            Source = new SvgImage { Source = source },
            Width = 12,
            Height = 12
        };
    }

    private void OnCommitItemPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not ListBoxItem item || _vm == null) return;

        var menu = new ContextMenu
        {
            Background = Avalonia.Media.Brush.Parse("#13131F"),
            BorderBrush = Avalonia.Media.Brush.Parse("#1E1E30"),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(4),
        };

        var m0 = new MenuItem
        {
            Header = "Copy hash",
            Command = _vm.CopyHashCommand,
            Icon = MakeSvgIcon("avares://gitclient/Assets/icons/copy.svg")
        };
        var m1 = new MenuItem
        {
            Header = "Copy short hash",
            Command = _vm.CopyShortHashCommand,
            Icon = MakeSvgIcon("avares://gitclient/Assets/icons/copy.svg")
        };
        var m2 = new MenuItem
        {
            Header = "Copy message",
            Command = _vm.CopyMessageCommand,
            Icon = MakeSvgIcon("avares://gitclient/Assets/icons/message.svg")
        };

        menu.Items.Add(m0);
        menu.Items.Add(m1);
        menu.Items.Add(m2);
        item.ContextMenu = menu;

        void UpdateParams()
        {
            if (item.DataContext is not GitCommit commit) return;
            m0.CommandParameter = commit.Sha;
            m1.CommandParameter = commit.ShortSha;
            m2.CommandParameter = commit.Message;
        }

        UpdateParams();
        item.DataContextChanged += (_, _) => UpdateParams();
    }
}