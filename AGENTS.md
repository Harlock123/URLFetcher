# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers the features and how to use the app;
this file covers how the code is put together.

## What this is

A small Avalonia 11 desktop app on .NET 9 that scans a web page for image links (`<a href>` and `<img src>`) and
downloads them into a chosen folder. MVVM with CommunityToolkit.Mvvm; HTML parsing with HtmlAgilityPack.

## Commands

```bash
dotnet build URLFetcher.sln                  # needs the .NET 9 SDK; there is no global.json
dotnet run --project URLFetcher              # no command-line arguments
```

There are no tests yet, and no CI workflow.

## Layout

- `URLFetcher/Services/IDownloadService.cs`: the service contract and the `DownloadProgress` and `DownloadResult`
  records.
- `URLFetcher/Services/DownloadService.cs`: the work.
  - `ExtractLinksAsync` fetches the page, resolves each link against the page URL, and keeps those whose extension is
    in `ImageExtensions`.
  - `DownloadFilesAsync` streams results one file at a time, reporting progress and renaming duplicates with numeric
    suffixes.
- `URLFetcher/ViewModels/MainWindowViewModel.cs`: the URL, folder, progress and status properties, the browse,
  download and cancel commands, plus `DownloadedFileViewModel` and `BoolToColorConverter`.
- `URLFetcher/Views/MainWindow.axaml(.cs)`: the window. The code-behind answers `FolderSelectionRequested` with
  Avalonia's `StorageProvider` folder picker.
- `ViewLocator.cs`, `App.axaml(.cs)`, `Program.cs`: the standard Avalonia template start-up.

## Rules that keep it working

- **The view model doesn't touch the UI.** It asks for a folder through the `FolderSelectionRequested` event and
  takes an `IDownloadService` in its constructor, so it can be driven without a window.
- **Cancellation flows through.** Both service methods take a `CancellationToken`; keep passing it to every network
  and file call so Cancel stops promptly.
- **Supported formats live in one place.** Add an image type to `ImageExtensions` in `DownloadService`, and to the
  README's list.
