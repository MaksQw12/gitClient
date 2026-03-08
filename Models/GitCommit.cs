using System;
using System.Collections.Generic;
using System.Text;

namespace gitclient.Models
{
    public class GitCommit
    {
        public string Sha { get; set; } = "";
        public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;
        public string Message { get; set; } = "";
        public string ShortMessage => Message.Split('\n')[0];
        public string Author { get; set; } = "";
        public DateTime Date { get; set; }
        public string DateFormatted => Date.ToString("dd MMM, HH:mm");
    }
}
