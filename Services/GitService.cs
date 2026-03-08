using gitclient.Models;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace gitclient.Services
{
    public class GitService
    {
        public static bool IsValidRepository(string path)
        {
            try { return Repository.IsValid(path); }
            catch { return false; }
        }

        public List<GitCommit> GetCommits(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                return repo.Commits
                    .Take(100)
                    .Select(c => new GitCommit
                    {
                        Sha = c.Sha,
                        Message = c.Message,
                        Author = c.Author.Name,
                        Date = c.Author.When.DateTime,
                    })
                    .ToList();
            }
            catch { return new(); }
        }

        public List<GitFileChange> GetChangedFiles(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var status = repo.RetrieveStatus();
                var result = new List<GitFileChange>();

                foreach (var entry in status)
                {
                    var state = entry.State;
                    string statusStr;

                    if (state.HasFlag(FileStatus.ModifiedInWorkdir) || state.HasFlag(FileStatus.ModifiedInIndex))
                        statusStr = "Modified";
                    else if (state.HasFlag(FileStatus.NewInWorkdir) || state.HasFlag(FileStatus.NewInIndex))
                        statusStr = "Added";
                    else if (state.HasFlag(FileStatus.DeletedFromWorkdir) || state.HasFlag(FileStatus.DeletedFromIndex))
                        statusStr = "Deleted";
                    else if (state.HasFlag(FileStatus.RenamedInWorkdir) || state.HasFlag(FileStatus.RenamedInIndex))
                        statusStr = "Renamed";
                    else
                        statusStr = state.ToString();

                    result.Add(new GitFileChange
                    {
                        FilePath = entry.FilePath,
                        Status = statusStr,
                        IsStaged = state.HasFlag(FileStatus.ModifiedInIndex)
                                || state.HasFlag(FileStatus.NewInIndex)
                                || state.HasFlag(FileStatus.DeletedFromIndex)
                    });
                }
                return result;
            }
            catch { return new(); }
        }

        public List<GitFileChange> GetCommitFiles(string repoPath, string sha)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var commit = repo.Lookup<Commit>(sha);
                if (commit == null) return new();

                var parent = commit.Parents.FirstOrDefault();
                var diff = parent == null
                    ? repo.Diff.Compare<TreeChanges>(null, commit.Tree)
                    : repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

                return diff.Select(d => new GitFileChange
                {
                    FilePath = d.Path,
                    Status = d.Status.ToString(),
                    IsStaged = true
                }).ToList();
            }
            catch { return new(); }
        }

        public string GetFileDiff(string repoPath, string sha, string filePath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var commit = repo.Lookup<Commit>(sha);
                if (commit == null) return "";

                var parent = commit.Parents.FirstOrDefault();
                var patch = parent == null
                    ? repo.Diff.Compare<Patch>(null, commit.Tree)
                    : repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);

                return patch[filePath]?.Patch ?? "";
            }
            catch { return ""; }
        }

        public void StageFile(string repoPath, string filePath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                Commands.Stage(repo, filePath);
            }
            catch { }
        }

        public void UnstageFile(string repoPath, string filePath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                Commands.Unstage(repo, filePath);
            }
            catch { }
        }

        public void CommitChanges(string repoPath, string message, string authorName, string authorEmail)
        {
            using var repo = new Repository(repoPath);
            var status = repo.RetrieveStatus();
            if (!status.IsDirty) throw new Exception("No staged changes to commit.");

            var staged = status.Where(e =>
                e.State.HasFlag(FileStatus.ModifiedInIndex) ||
                e.State.HasFlag(FileStatus.NewInIndex) ||
                e.State.HasFlag(FileStatus.DeletedFromIndex)).ToList();

            if (!staged.Any()) throw new Exception("No staged changes to commit. Stage files first.");

            var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            repo.Commit(message, sig, sig);
        }

        public void Fetch(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                foreach (var remote in repo.Network.Remotes)
                    Commands.Fetch(repo, remote.Name, Array.Empty<string>(), null, null);
            }
            catch { }
        }

        public void Pull(string repoPath, string authorName, string authorEmail)
        {
            using var repo = new Repository(repoPath);
            var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            Commands.Pull(repo, sig, new PullOptions());
        }

        public void Push(string repoPath)
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Head;
            var options = new PushOptions
            {
                CredentialsProvider = (url, usernameFromUrl, types) =>
                {
                    var result = RunGitCredential(url);
                    return new UsernamePasswordCredentials
                    {
                        Username = result.username,
                        Password = result.password
                    };
                }
            };
            repo.Network.Push(branch, options);
        }

        private (string username, string password) RunGitCredential(string url)
        {
            var psi = new ProcessStartInfo("git", "credential fill")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            process.StandardInput.WriteLine($"url={url}");
            process.StandardInput.WriteLine();
            process.StandardInput.Close();

            string username = "", password = "";
            string line;
            while ((line = process.StandardOutput.ReadLine()!) != null)
            {
                if (line.StartsWith("username=")) username = line[9..];
                if (line.StartsWith("password=")) password = line[9..];
            }
            return (username, password);
        }

        public string GetCurrentBranch(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                return repo.Head.FriendlyName;
            }
            catch { return "unknown"; }
        }

        public List<string> GetBranches(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);

                var localBranches = repo.Branches
                    .Where(b => !b.IsRemote)
                    .Select(b => b.FriendlyName)
                    .ToList();

                var localNames = localBranches.ToHashSet();
                var remoteBranches = repo.Branches
                    .Where(b => b.IsRemote
                             && !b.FriendlyName.EndsWith("/HEAD")
                             && b.FriendlyName != "HEAD")
                    .Select(b => b.FriendlyName)
                    .Where(name =>
                    {
                        var localEquivalent = name.Contains("/")
                            ? name.Substring(name.IndexOf('/') + 1)
                            : name;
                        return !localNames.Contains(localEquivalent);
                    })
                    .ToList();

                return localBranches.Concat(remoteBranches).ToList();
            }
            catch { return new(); }
        }

        public string GetWorkingDiff(string repoPath, string filePath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var patch = repo.Diff.Compare<Patch>(
                    repo.Head.Tip?.Tree,
                    DiffTargets.WorkingDirectory | DiffTargets.Index
                );
                return patch[filePath]?.Patch ?? "";
            }
            catch { return ""; }
        }

        public (int ahead, int behind) GetAheadBehind(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var branch = repo.Head;
                if (branch.TrackedBranch == null) return (0, 0);
                var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(
                    branch.Tip, branch.TrackedBranch.Tip);
                return ((int)(divergence.AheadBy ?? 0), (int)(divergence.BehindBy ?? 0));
            }
            catch { return (0, 0); }
        }

        public List<StashItem> GetStashes(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                return repo.Stashes
                    .Select((s, i) => new StashItem
                    {
                        Index = i,
                        Message = s.Message,
                        Sha = s.WorkTree.Sha
                    })
                    .ToList();
            }
            catch { return new(); }
        }

        public void StashSave(string repoPath, string message, string authorName, string authorEmail)
        {
            using var repo = new Repository(repoPath);
            var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            repo.Stashes.Add(sig, message, StashModifiers.Default);
        }

        public void StashPop(string repoPath, int index)
        {
            using var repo = new Repository(repoPath);
            repo.Stashes.Pop(index, new StashApplyOptions());
        }

        public void StashDrop(string repoPath, int index)
        {
            using var repo = new Repository(repoPath);
            repo.Stashes.Remove(index);
        }
    }


}