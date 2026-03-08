# Kommit

> A fast, minimal Git client built with Avalonia UI and C#.

![Version](https://img.shields.io/badge/version-v0.2_alpha-7C6AF7?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

---

## What is this?

Kommit is a lightweight desktop Git client focused on simplicity and speed.  
No Electron, no bloat — just a native C# app that does what you need and stays out of your way.

---

## Features

**Core workflow**
- 3-column layout — Commits / Files / Diff in one view
- Stage, unstage, discard changes per file or all at once
- Commit, Push, Pull, Fetch
- Stash — save and restore WIP instantly

**Branch management**
- Create, rename, delete, switch branches
- Remote branch checkout — auto-creates local tracking branch
- Ahead/behind counter (`↑2 ↓3`) updates after Fetch

**Repository**
- Clone with GitHub repo dropdown (via personal access token)
- Drag & drop a folder to open
- Recent repositories list
- File watcher — changes appear automatically, no manual refresh needed

**UX**
- Diff author — who last touched the file, shown in the diff header
- Toast notifications for every operation
- Clean dark theme
- Settings stored at `%AppData%\Kommit\settings.json`

---

## Stack

| | |
|---|---|
| UI Framework | [Avalonia 11](https://avaloniaui.net/) |
| Git | [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Icons | [Avalonia.Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) |
| Runtime | .NET 9 / Windows |

---

## Build & Run

### Requirements
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11
- Git installed and available in PATH
```bash
git clone https://github.com/yourusername/kommit.git
cd kommit/gitclient
dotnet run
```

### Single-file publish
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Roadmap

**v0.3**
- [ ] AI commit message generation (Groq — llama-3.3-70b)
- [ ] Search / filter commits
- [ ] Auto-fetch on interval
- [ ] Context menu on files (right-click stage/discard)

**v0.4**
- [ ] Commit graph visualization
- [ ] File history
- [ ] Git Blame
- [ ] Cherry-pick
- [ ] Syntax highlighting in diff

**Future**
- [ ] macOS / Linux support
- [ ] Multiple repositories (tabs)
- [ ] Stash diff preview

---

## License

See [LICENSE](LICENSE) for details.