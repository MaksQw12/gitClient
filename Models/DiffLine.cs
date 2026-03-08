using Avalonia.Media;

namespace gitclient.Models
{
    public class DiffLine
    {
        public string Text { get; set; } = "";
        public DiffLineType Type { get; set; }

        public IBrush Foreground => Type switch
        {
            DiffLineType.Added => new SolidColorBrush(Color.Parse("#5EE89A")),
            DiffLineType.Removed => new SolidColorBrush(Color.Parse("#FF6B6B")),
            DiffLineType.Header => new SolidColorBrush(Color.Parse("#4FC8FF")),
            DiffLineType.Meta => new SolidColorBrush(Color.Parse("#555570")),
            _ => new SolidColorBrush(Color.Parse("#7070A0")),
        };

        public IBrush Background => Type switch
        {
            DiffLineType.Added => new SolidColorBrush(Color.Parse("#0D2E1A")),
            DiffLineType.Removed => new SolidColorBrush(Color.Parse("#2E0D0D")),
            _ => new SolidColorBrush(Colors.Transparent),
        };
    }

    public enum DiffLineType
    {
        Normal,
        Added,
        Removed,
        Header,
        Meta
    }
}