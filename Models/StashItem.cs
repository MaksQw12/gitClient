namespace gitclient.Models
{
    public class StashItem
    {
        public int Index { get; set; }
        public string Message { get; set; } = "";
        public string Sha { get; set; } = "";
        public string ShortMessage => Message.Length > 50 ? Message[..50] + "..." : Message;
    }
}
